using FinanceApp.Application.DTOs.CreditCard;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class CreditCardService : ICreditCardService
{
    private const string ExpenseSource = "expense";
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IFinancialAccountService _accountService;
    private readonly IUnitOfWork _unitOfWork;

    public CreditCardService(
        ICreditCardRepository creditCardRepository,
        IExpenseRepository expenseRepository,
        ICategoryRepository categoryRepository,
        IFinancialAccountService accountService,
        IUnitOfWork unitOfWork)
    {
        _creditCardRepository = creditCardRepository;
        _expenseRepository = expenseRepository;
        _categoryRepository = categoryRepository;
        _accountService = accountService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CreditCardResponseDto>> GetAllAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        (await _creditCardRepository.GetByUserIdAsync(userId, cancellationToken))
            .Select(MapCard).ToList();

    public async Task<CreditCardResponseDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        MapCard(await GetOwnedCardAsync(id, userId, false, cancellationToken));

    public async Task<CreditCardResponseDto> CreateAsync(
        Guid userId, CreditCardCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateCard(dto.Name, dto.OpeningBalance, dto.CreditLimit,
            dto.ClosingDay, dto.DueDay, dto.OpeningDate);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var card = new CreditCard
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = dto.Name.Trim(),
                CurrentBalance = dto.OpeningBalance,
                CreditLimit = dto.CreditLimit,
                ClosingDay = dto.ClosingDay,
                DueDay = dto.DueDay,
                Notes = dto.Notes?.Trim(),
                IsActive = true
            };
            await _creditCardRepository.CreateAsync(card, ct);

            if (dto.OpeningBalance > 0)
            {
                await _creditCardRepository.SaveTransactionAsync(new CreditCardTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreditCardId = card.Id,
                    Type = CreditCardTransactionType.OpeningBalance,
                    Amount = dto.OpeningBalance,
                    Date = dto.OpeningDate,
                    Description = "Saldo inicial de tarjeta",
                    SourceType = "credit-card-opening",
                    SourceId = card.Id
                }, ct);
            }

            return MapCard(card);
        }, cancellationToken);
    }

    public async Task<CreditCardResponseDto> UpdateAsync(
        Guid id, Guid userId, CreditCardUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateCard(dto.Name, 0, dto.CreditLimit, dto.ClosingDay, dto.DueDay, null);
        var card = await GetOwnedCardAsync(id, userId, false, cancellationToken);
        card.Name = dto.Name.Trim();
        card.CreditLimit = dto.CreditLimit;
        card.ClosingDay = dto.ClosingDay;
        card.DueDay = dto.DueDay;
        card.IsActive = dto.IsActive;
        card.Notes = dto.Notes?.Trim();
        await _creditCardRepository.UpdateAsync(card, cancellationToken);
        return MapCard(card);
    }

    public async Task<IReadOnlyList<CreditCardTransactionResponseDto>> GetTransactionsAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var card = await GetOwnedCardAsync(id, userId, false, cancellationToken);
        return card.Transactions
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
            .Select(MapTransaction).ToList();
    }

    public async Task<IReadOnlyList<CreditCardPaymentResponseDto>> GetPaymentsAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var card = await GetOwnedCardAsync(id, userId, false, cancellationToken);
        return card.Payments
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.CreatedAt)
            .Select(MapPayment).ToList();
    }

    public async Task<CreditCardPaymentResponseDto> AddPaymentAsync(
        Guid id, Guid userId, CreditCardPaymentCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidatePayment(dto);
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var duplicate = await _creditCardRepository.GetPaymentByIdempotencyKeyAsync(
                userId, id, dto.IdempotencyKey, ct);
            if (duplicate != null)
            {
                EnsureSamePayment(duplicate, dto);
                return MapPayment(duplicate);
            }

            var card = await GetOwnedCardAsync(id, userId, true, ct);
            if (dto.PrincipalAmount > card.CurrentBalance)
                throw new DomainException("CREDIT_CARD_OVERPAYMENT",
                    "El pago a capital no puede superar el saldo actual de la tarjeta.");

            var totalCashOut = dto.PrincipalAmount + dto.CommissionAmount;
            var available = await _accountService.GetAvailableBalanceAsync(
                userId, dto.SourceAccountId, FinancialAccountType.Cash, ct);
            if (available < totalCashOut)
                throw new DomainException("INSUFFICIENT_ACCOUNT_BALANCE",
                    "La cuenta seleccionada no tiene saldo suficiente para el pago y su comisión.");

            Category? commissionCategory = null;
            if (dto.CommissionAmount > 0)
                commissionCategory = await GetExpenseCategoryAsync(
                    dto.CommissionCategoryId!.Value, userId, ct);

            card.CurrentBalance -= dto.PrincipalAmount;
            await _creditCardRepository.UpdateAsync(card, ct);

            var payment = new CreditCardPayment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreditCardId = card.Id,
                SourceAccountId = dto.SourceAccountId,
                PrincipalAmount = dto.PrincipalAmount,
                CommissionAmount = dto.CommissionAmount,
                CardBalanceAfter = card.CurrentBalance,
                PaymentDate = dto.PaymentDate,
                Notes = dto.Notes?.Trim(),
                IdempotencyKey = dto.IdempotencyKey
            };
            await _creditCardRepository.SavePaymentAsync(payment, ct);

            await _creditCardRepository.SaveTransactionAsync(new CreditCardTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreditCardId = card.Id,
                Type = CreditCardTransactionType.Payment,
                Amount = -dto.PrincipalAmount,
                Date = dto.PaymentDate,
                Description = $"Pago de tarjeta {card.Name}",
                SourceType = "credit-card-payment",
                SourceId = payment.Id
            }, ct);
            await _accountService.SyncMovementAsync(
                userId, dto.SourceAccountId, FinancialAccountType.Cash,
                -dto.PrincipalAmount, dto.PaymentDate, "credit-card-payment", payment.Id,
                $"Pago de tarjeta {card.Name}", ct);

            if (dto.CommissionAmount > 0)
            {
                var commission = new Expense
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CategoryId = commissionCategory!.Id,
                    AccountId = dto.SourceAccountId,
                    Amount = dto.CommissionAmount,
                    Description = $"Comisión por pago de tarjeta {card.Name}",
                    Date = dto.PaymentDate,
                    PaymentMethod = PaymentMethod.Other,
                    Notes = dto.Notes?.Trim()
                };
                await _expenseRepository.CreateAsync(commission, ct);
                await _accountService.SyncMovementAsync(
                    userId, dto.SourceAccountId, FinancialAccountType.Cash,
                    -dto.CommissionAmount, dto.PaymentDate, ExpenseSource, commission.Id,
                    commission.Description, ct);
                payment.CommissionExpenseId = commission.Id;
                payment.CommissionExpense = commission;
                await _creditCardRepository.SavePaymentAsync(payment, ct);
            }

            payment.CreditCard = card;
            return MapPayment(payment);
        }, cancellationToken);
    }

    public async Task<CreditCardPaymentResponseDto> VoidPaymentAsync(
        Guid id, Guid paymentId, Guid userId, CreditCardPaymentVoidDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.IdempotencyKey == Guid.Empty)
            throw new DomainException("INVALID_IDEMPOTENCY_KEY", "La clave de idempotencia es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length > 500)
            throw new DomainException("INVALID_VOID_REASON", "Indica un motivo de hasta 500 caracteres.");
        ValidateDate(dto.Date, "La fecha de anulación");

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var payment = await _creditCardRepository.GetPaymentByIdAsync(
                paymentId, id, userId, ct);
            if (payment == null)
                throw new NotFoundException("Pago de tarjeta", paymentId);
            if (payment.IsVoided)
            {
                if (payment.VoidIdempotencyKey == dto.IdempotencyKey)
                    return MapPayment(payment);
                throw new DomainException("CREDIT_CARD_PAYMENT_ALREADY_VOIDED",
                    "El pago ya fue anulado y conserva su historial.");
            }

            var card = await GetOwnedCardAsync(id, userId, false, ct);
            card.CurrentBalance += payment.PrincipalAmount;
            await _creditCardRepository.UpdateAsync(card, ct);

            await _accountService.SyncMovementAsync(
                userId, payment.SourceAccountId, FinancialAccountType.Cash,
                0m, dto.Date, "credit-card-payment", payment.Id,
                $"Anulación de pago de tarjeta {card.Name}", ct);

            if (payment.CommissionAmount > 0 && payment.CommissionExpenseId.HasValue)
            {
                await _accountService.SyncMovementAsync(
                    userId, payment.SourceAccountId, FinancialAccountType.Cash,
                    0m, dto.Date, ExpenseSource, payment.CommissionExpenseId.Value,
                    $"Anulación de comisión de tarjeta {card.Name}", ct);
                if (payment.CommissionExpense is { IsDeleted: false } commission)
                {
                    commission.DeletedAt = DateTimeOffset.UtcNow;
                    commission.UpdatedAt = DateTimeOffset.UtcNow;
                    await _expenseRepository.UpdateAsync(commission, ct);
                }
            }

            await _creditCardRepository.SaveTransactionAsync(new CreditCardTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreditCardId = card.Id,
                Type = CreditCardTransactionType.PaymentReversal,
                Amount = payment.PrincipalAmount,
                Date = dto.Date,
                Description = $"Anulación de pago: {dto.Reason.Trim()}",
                SourceType = "credit-card-payment-reversal",
                SourceId = payment.Id
            }, ct);

            payment.VoidedAt = DateTimeOffset.UtcNow;
            payment.VoidReason = dto.Reason.Trim();
            payment.VoidIdempotencyKey = dto.IdempotencyKey;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            await _creditCardRepository.SavePaymentAsync(payment, ct);
            payment.CreditCard = card;
            return MapPayment(payment);
        }, cancellationToken);
    }

    public async Task<CreditCardTransactionResponseDto> AddChargeAsync(
        Guid id, Guid userId, CreditCardChargeCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CreditCardTransactionType>(dto.Type, true, out var type)
            || type is not (CreditCardTransactionType.Interest or CreditCardTransactionType.Fee))
            throw new DomainException("INVALID_CREDIT_CARD_CHARGE",
                "El cargo debe ser de tipo interest o fee.");
        if (dto.Amount <= 0)
            throw new DomainException("INVALID_CREDIT_CARD_CHARGE_AMOUNT", "El monto debe ser mayor que cero.");
        if (dto.IdempotencyKey == Guid.Empty)
            throw new DomainException("INVALID_IDEMPOTENCY_KEY", "La clave de idempotencia es obligatoria.");
        ValidateDate(dto.Date, "La fecha del cargo");

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var duplicateExpense = await _expenseRepository.GetByIdempotencyKeyAsync(
                userId, dto.IdempotencyKey, ct);
            if (duplicateExpense != null)
            {
                if (duplicateExpense.CreditCardId != id || duplicateExpense.Amount != dto.Amount
                    || duplicateExpense.Date != dto.Date)
                    throw new DomainException("IDEMPOTENCY_KEY_REUSE",
                        "La clave de idempotencia ya fue usada con datos diferentes.");
                var existingTransaction = await _creditCardRepository.GetTransactionBySourceAsync(
                    userId, ExpenseSource, duplicateExpense.Id, ct);
                if (existingTransaction == null)
                    throw new DomainException("INCOMPLETE_CREDIT_CARD_OPERATION",
                        "El cargo idempotente existe sin su movimiento de pasivo y requiere revisión.");
                return MapTransaction(existingTransaction);
            }

            var card = await GetOwnedCardAsync(id, userId, true, ct);
            var category = await GetExpenseCategoryAsync(dto.CategoryId, userId, ct);
            var description = string.IsNullOrWhiteSpace(dto.Description)
                ? type == CreditCardTransactionType.Interest
                    ? $"Interés de tarjeta {card.Name}"
                    : $"Comisión de tarjeta {card.Name}"
                : dto.Description.Trim();

            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CategoryId = category.Id,
                CreditCardId = card.Id,
                IdempotencyKey = dto.IdempotencyKey,
                Amount = dto.Amount,
                Description = description,
                Date = dto.Date,
                PaymentMethod = PaymentMethod.CreditCard
            };
            await _expenseRepository.CreateAsync(expense, ct);
            await SyncExpenseAsync(userId, card.Id, expense.Id, expense.Amount,
                expense.Date, description, type, ct);
            var transaction = await _creditCardRepository.GetTransactionBySourceAsync(
                userId, ExpenseSource, expense.Id, ct);
            return MapTransaction(transaction!);
        }, cancellationToken);
    }

    public async Task SyncExpenseAsync(
        Guid userId, Guid creditCardId, Guid expenseId, decimal amount,
        DateOnly date, string description,
        CreditCardTransactionType type = CreditCardTransactionType.Purchase,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new DomainException("INVALID_CREDIT_CARD_AMOUNT", "El monto debe ser mayor que cero.");

        var targetCard = await GetOwnedCardAsync(creditCardId, userId, true, cancellationToken);
        var existing = await _creditCardRepository.GetTransactionBySourceAsync(
            userId, ExpenseSource, expenseId, cancellationToken);

        if (existing == null)
        {
            targetCard.CurrentBalance += amount;
            await _creditCardRepository.UpdateAsync(targetCard, cancellationToken);
            await _creditCardRepository.SaveTransactionAsync(new CreditCardTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreditCardId = targetCard.Id,
                Type = type,
                Amount = amount,
                Date = date,
                Description = description,
                SourceType = ExpenseSource,
                SourceId = expenseId
            }, cancellationToken);
            return;
        }

        if (existing.CreditCardId == targetCard.Id)
        {
            targetCard.CurrentBalance += amount - existing.Amount;
            await _creditCardRepository.UpdateAsync(targetCard, cancellationToken);
        }
        else
        {
            var previousCard = await GetOwnedCardAsync(
                existing.CreditCardId, userId, false, cancellationToken);
            previousCard.CurrentBalance -= existing.Amount;
            targetCard.CurrentBalance += amount;
            await _creditCardRepository.UpdateAsync(previousCard, cancellationToken);
            await _creditCardRepository.UpdateAsync(targetCard, cancellationToken);
        }

        existing.CreditCardId = targetCard.Id;
        if (existing.Type is not (CreditCardTransactionType.Interest or CreditCardTransactionType.Fee)
            || type != CreditCardTransactionType.Purchase) existing.Type = type;
        existing.Amount = amount;
        existing.Date = date;
        existing.Description = description;
        await _creditCardRepository.SaveTransactionAsync(existing, cancellationToken);
    }

    public async Task SyncReimbursementAsync(
        Guid userId, Guid creditCardId, Guid reimbursementId, decimal amount,
        DateOnly date, string description, CancellationToken cancellationToken = default)
    {
        var existing = await _creditCardRepository.GetTransactionBySourceAsync(
            userId, "reimbursement", reimbursementId, cancellationToken);
        if (amount == 0)
        {
            if (existing == null) return;
            var existingCard = await GetOwnedCardAsync(existing.CreditCardId, userId, false, cancellationToken);
            existingCard.CurrentBalance -= existing.Amount;
            existing.DeletedAt = DateTimeOffset.UtcNow;
            await _creditCardRepository.UpdateAsync(existingCard, cancellationToken);
            await _creditCardRepository.SaveTransactionAsync(existing, cancellationToken);
            return;
        }

        var targetCard = await GetOwnedCardAsync(creditCardId, userId, true, cancellationToken);
        var signedAmount = -amount;
        if (existing == null)
        {
            targetCard.CurrentBalance += signedAmount;
            await _creditCardRepository.UpdateAsync(targetCard, cancellationToken);
            await _creditCardRepository.SaveTransactionAsync(new CreditCardTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreditCardId = targetCard.Id,
                Type = CreditCardTransactionType.Refund,
                Amount = signedAmount,
                Date = date,
                Description = description,
                SourceType = "reimbursement",
                SourceId = reimbursementId
            }, cancellationToken);
            return;
        }

        if (existing.CreditCardId == targetCard.Id)
        {
            targetCard.CurrentBalance += signedAmount - existing.Amount;
            await _creditCardRepository.UpdateAsync(targetCard, cancellationToken);
        }
        else
        {
            var previousCard = await GetOwnedCardAsync(existing.CreditCardId, userId, false, cancellationToken);
            previousCard.CurrentBalance -= existing.Amount;
            targetCard.CurrentBalance += signedAmount;
            await _creditCardRepository.UpdateAsync(previousCard, cancellationToken);
            await _creditCardRepository.UpdateAsync(targetCard, cancellationToken);
        }

        existing.CreditCardId = targetCard.Id;
        existing.Type = CreditCardTransactionType.Refund;
        existing.Amount = signedAmount;
        existing.Date = date;
        existing.Description = description;
        await _creditCardRepository.SaveTransactionAsync(existing, cancellationToken);
    }

    public async Task RemoveExpenseAsync(
        Guid userId, Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _creditCardRepository.GetTransactionBySourceAsync(
            userId, ExpenseSource, expenseId, cancellationToken);
        if (transaction == null)
            return;

        var card = await GetOwnedCardAsync(
            transaction.CreditCardId, userId, false, cancellationToken);
        card.CurrentBalance -= transaction.Amount;
        transaction.DeletedAt = DateTimeOffset.UtcNow;
        await _creditCardRepository.UpdateAsync(card, cancellationToken);
        await _creditCardRepository.SaveTransactionAsync(transaction, cancellationToken);
    }

    private async Task<CreditCard> GetOwnedCardAsync(
        Guid id, Guid userId, bool requireActive, CancellationToken cancellationToken)
    {
        var card = await _creditCardRepository.GetByIdWithHistoryAsync(id, userId, cancellationToken);
        if (card == null)
            throw new NotFoundException("Tarjeta de crédito", id);
        if (requireActive && !card.IsActive)
            throw new DomainException("INACTIVE_CREDIT_CARD", "La tarjeta seleccionada está inactiva.");
        return card;
    }

    private async Task<Category> GetExpenseCategoryAsync(
        Guid categoryId, Guid userId, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category == null || category.IsDeleted
            || (category.UserId.HasValue && category.UserId != userId)
            || category.Type == CategoryType.Income)
            throw new DomainException("INVALID_EXPENSE_CATEGORY",
                "La categoría de gasto no existe o no pertenece al usuario.");
        return category;
    }

    private static void ValidateCard(
        string name, decimal openingBalance, decimal? creditLimit,
        int closingDay, int dueDay, DateOnly? openingDate)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
            throw new DomainException("INVALID_CREDIT_CARD_NAME", "El nombre de la tarjeta es obligatorio y admite hasta 120 caracteres.");
        if (openingBalance < 0)
            throw new DomainException("INVALID_OPENING_BALANCE", "El saldo inicial no puede ser negativo.");
        if (creditLimit.HasValue && creditLimit <= 0)
            throw new DomainException("INVALID_CREDIT_LIMIT", "El límite de crédito debe ser mayor que cero.");
        if (creditLimit.HasValue && openingBalance > creditLimit.Value)
            throw new DomainException("OPENING_BALANCE_EXCEEDS_LIMIT", "El saldo inicial no puede superar el límite de crédito.");
        if (closingDay is < 1 or > 31 || dueDay is < 1 or > 31)
            throw new DomainException("INVALID_CREDIT_CARD_DAY", "Los días de corte y vencimiento deben estar entre 1 y 31.");
        if (openingDate.HasValue)
            ValidateDate(openingDate.Value, "La fecha de apertura");
    }

    private static void ValidatePayment(CreditCardPaymentCreateDto dto)
    {
        if (dto.IdempotencyKey == Guid.Empty)
            throw new DomainException("INVALID_IDEMPOTENCY_KEY", "La clave de idempotencia es obligatoria.");
        if (dto.SourceAccountId == Guid.Empty)
            throw new DomainException("INVALID_ACCOUNT", "Selecciona la cuenta desde la que se pagó.");
        if (dto.PrincipalAmount <= 0)
            throw new DomainException("INVALID_PRINCIPAL_AMOUNT", "El pago a capital debe ser mayor que cero.");
        if (dto.CommissionAmount < 0)
            throw new DomainException("INVALID_COMMISSION_AMOUNT", "La comisión no puede ser negativa.");
        if (dto.CommissionAmount > 0 && !dto.CommissionCategoryId.HasValue)
            throw new DomainException("COMMISSION_CATEGORY_REQUIRED", "Selecciona una categoría para la comisión financiera.");
        ValidateDate(dto.PaymentDate, "La fecha del pago");
    }

    private static void ValidateDate(DateOnly date, string label)
    {
        if (date == default || date > DateOnly.FromDateTime(DateTime.Today))
            throw new DomainException("INVALID_TRANSACTION_DATE", $"{label} es obligatoria y no puede ser futura.");
    }

    private static void EnsureSamePayment(
        CreditCardPayment payment, CreditCardPaymentCreateDto dto)
    {
        if (payment.SourceAccountId != dto.SourceAccountId
            || payment.PrincipalAmount != dto.PrincipalAmount
            || payment.CommissionAmount != dto.CommissionAmount
            || payment.PaymentDate != dto.PaymentDate)
            throw new DomainException("IDEMPOTENCY_KEY_REUSE",
                "La clave de idempotencia ya fue usada con datos diferentes.");
    }

    private static CreditCardResponseDto MapCard(CreditCard card) => new()
    {
        Id = card.Id,
        Name = card.Name,
        CurrentBalance = card.CurrentBalance,
        CreditLimit = card.CreditLimit,
        AvailableCredit = card.CreditLimit.HasValue
            ? Math.Max(0, card.CreditLimit.Value - Math.Max(0, card.CurrentBalance))
            : null,
        ClosingDay = card.ClosingDay,
        DueDay = card.DueDay,
        IsActive = card.IsActive,
        Notes = card.Notes,
        CreatedAt = card.CreatedAt
    };

    private static CreditCardTransactionResponseDto MapTransaction(
        CreditCardTransaction transaction) => new()
    {
        Id = transaction.Id,
        Type = ToSnakeCase(transaction.Type.ToString()),
        Amount = transaction.Amount,
        Date = transaction.Date,
        Description = transaction.Description,
        SourceType = transaction.SourceType,
        SourceId = transaction.SourceId,
        CreatedAt = transaction.CreatedAt
    };

    private static CreditCardPaymentResponseDto MapPayment(CreditCardPayment payment) => new()
    {
        Id = payment.Id,
        CreditCardId = payment.CreditCardId,
        SourceAccountId = payment.SourceAccountId,
        SourceAccountName = payment.SourceAccount?.Name ?? string.Empty,
        PrincipalAmount = payment.PrincipalAmount,
        CommissionAmount = payment.CommissionAmount,
        CommissionExpenseId = payment.CommissionExpenseId,
        PaymentDate = payment.PaymentDate,
        Notes = payment.Notes,
        IdempotencyKey = payment.IdempotencyKey,
        IsVoided = payment.IsVoided,
        VoidedAt = payment.VoidedAt,
        VoidReason = payment.VoidReason,
        VoidIdempotencyKey = payment.VoidIdempotencyKey,
        CardBalanceAfter = payment.CardBalanceAfter,
        CreatedAt = payment.CreatedAt
    };

    private static string ToSnakeCase(string value) => string.Concat(
        value.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c : c.ToString()))
        .ToLowerInvariant();
}
