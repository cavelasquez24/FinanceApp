using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Expense;
using FinanceApp.Application.DTOs.Tag;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Domain.Models;

namespace FinanceApp.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IFinancialAccountService _accountService;
    private readonly ITagRepository _tagRepository;
    private readonly ICreditCardService _creditCardService;
    private readonly IUnitOfWork _unitOfWork;

    public ExpenseService(
        IExpenseRepository expenseRepository,
        ITagRepository tagRepository,
        IFinancialAccountService accountService,
        ICreditCardService creditCardService,
        IUnitOfWork unitOfWork)
    {
        _expenseRepository = expenseRepository;
        _tagRepository = tagRepository;
        _accountService = accountService;
        _creditCardService = creditCardService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<ExpenseResponseDto>> GetAllAsync(
        Guid userId,
        ExpenseFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        PaymentMethod? paymentMethod = string.IsNullOrWhiteSpace(filter.PaymentMethod)
            ? null
            : Enum.Parse<PaymentMethod>(filter.PaymentMethod.Replace("_", ""), true);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var options = new ExpenseQueryOptions
        {
            Page = page,
            PageSize = pageSize,
            CategoryId = filter.CategoryId,
            StartDate = filter.StartDate,
            EndDate = filter.EndDate,
            MinAmount = filter.MinAmount,
            MaxAmount = filter.MaxAmount,
            Search = filter.Search,
            PaymentMethod = paymentMethod,
            IsRecurring = filter.IsRecurring,
            SortBy = filter.SortBy,
            SortDescending = !filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase),
            TagIds = filter.TagIds.Distinct().ToArray(),
            MatchAllTags = filter.TagMatch.Equals("all", StringComparison.OrdinalIgnoreCase),
            Untagged = filter.Untagged,
            Merchant = filter.Merchant
        };
        var (items, totalCount) = await _expenseRepository.GetFilteredAsync(
            userId, options, cancellationToken);

        return new PagedResponse<ExpenseResponseDto>
        {
            Items = items.Select(MapToResponseDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ExpenseResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense == null || expense.UserId != userId || expense.IsDeleted)
            throw new NotFoundException("Gasto", id);

        return MapToResponseDto(expense);
    }

    public async Task<ExpenseResponseDto> CreateAsync(
        Guid userId,
        ExpenseCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.IdempotencyKey.HasValue)
        {
            var duplicate = await _expenseRepository.GetByIdempotencyKeyAsync(
                userId, dto.IdempotencyKey.Value, cancellationToken);
            if (duplicate != null)
            {
                EnsureSameExpense(duplicate, dto);
                return MapToResponseDto(duplicate);
            }
        }

        var paymentMethod = await ValidatePaymentSourceAsync(
            userId, dto.PaymentMethod, dto.AccountId, dto.CreditCardId, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var tags = await ResolveTagsAsync(userId, dto.TagIds, ct);
            RecurrenceType? recurrenceType = dto.RecurrenceType != null
                ? Enum.Parse<RecurrenceType>(
                    dto.RecurrenceType.Replace("_", ""),
                    ignoreCase: true)
                : null;

            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CategoryId = dto.CategoryId,
                AccountId = paymentMethod == PaymentMethod.CreditCard ? null : dto.AccountId,
                CreditCardId = paymentMethod == PaymentMethod.CreditCard ? dto.CreditCardId : null,
                IdempotencyKey = dto.IdempotencyKey,
                Amount = dto.Amount,
                Merchant = dto.Merchant?.Trim(),
                ExpenseTags = tags.Select(t => new ExpenseTag { TagId = t.Id }).ToList(),
                Description = dto.Description?.Trim(),
                Date = dto.Date,
                PaymentMethod = paymentMethod,
                IsRecurring = dto.IsRecurring,
                RecurrenceType = recurrenceType,
                Notes = dto.Notes?.Trim()
            };

            await _expenseRepository.CreateAsync(expense, ct);
            var description = dto.Description?.Trim() ?? "Gasto";
            if (paymentMethod == PaymentMethod.CreditCard)
            {
                await _creditCardService.SyncExpenseAsync(
                    userId, dto.CreditCardId!.Value, expense.Id, dto.Amount,
                    dto.Date, description, CreditCardTransactionType.Purchase, ct);
            }
            else
            {
                await _accountService.SyncMovementAsync(
                    userId, dto.AccountId, FinancialAccountType.Cash, -dto.Amount,
                    dto.Date, "expense", expense.Id, description, ct);
            }

            return await GetByIdAsync(expense.Id, userId, ct);
        }, cancellationToken);
    }

    public async Task<ExpenseResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        ExpenseUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);
        if (expense == null || expense.UserId != userId || expense.IsDeleted)
            throw new NotFoundException("Gasto", id);

        var previousCreditCardId = expense.CreditCardId;
        var previousAccountId = expense.AccountId;
        var paymentMethod = await ValidatePaymentSourceAsync(
            userId, dto.PaymentMethod, dto.AccountId, dto.CreditCardId, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var tags = await ResolveTagsAsync(userId, dto.TagIds, ct);
            expense.Merchant = dto.Merchant?.Trim();
            expense.CategoryId = dto.CategoryId;
            expense.AccountId = paymentMethod == PaymentMethod.CreditCard ? null : dto.AccountId;
            expense.CreditCardId = paymentMethod == PaymentMethod.CreditCard ? dto.CreditCardId : null;
            expense.Amount = dto.Amount;
            expense.Description = dto.Description?.Trim();
            expense.Date = dto.Date;
            expense.PaymentMethod = paymentMethod;
            expense.IsRecurring = dto.IsRecurring;
            expense.RecurrenceType = dto.RecurrenceType != null
                ? Enum.Parse<RecurrenceType>(
                    dto.RecurrenceType.Replace("_", ""),
                    ignoreCase: true)
                : null;
            expense.Notes = dto.Notes?.Trim();
            expense.ExpenseTags.Clear();
            foreach (var tag in tags)
                expense.ExpenseTags.Add(new ExpenseTag { ExpenseId = expense.Id, TagId = tag.Id });

            await _expenseRepository.UpdateAsync(expense, ct);
            var description = dto.Description?.Trim() ?? "Gasto";
            if (previousCreditCardId.HasValue && paymentMethod != PaymentMethod.CreditCard)
            {
                await _creditCardService.RemoveExpenseAsync(userId, expense.Id, ct);
            }
            else if (!previousCreditCardId.HasValue && paymentMethod == PaymentMethod.CreditCard)
            {
                await _accountService.SyncMovementAsync(
                    userId, previousAccountId, FinancialAccountType.Cash, 0,
                    expense.Date, "expense", expense.Id, "Gasto cambiado a crédito", ct);
            }

            if (paymentMethod == PaymentMethod.CreditCard)
            {
                await _creditCardService.SyncExpenseAsync(
                    userId, dto.CreditCardId!.Value, expense.Id, dto.Amount,
                    dto.Date, description, CreditCardTransactionType.Purchase, ct);
            }
            else
            {
                await _accountService.SyncMovementAsync(
                    userId, dto.AccountId, FinancialAccountType.Cash, -dto.Amount,
                    dto.Date, "expense", expense.Id, description, ct);
            }

            return await GetByIdAsync(expense.Id, userId, ct);
        }, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var expense = await _expenseRepository.GetByIdAsync(id, ct);
            if (expense == null || expense.UserId != userId || expense.IsDeleted)
                throw new NotFoundException("Gasto", id);

            expense.DeletedAt = DateTimeOffset.UtcNow;
            await _expenseRepository.UpdateAsync(expense, ct);
            if (expense.CreditCardId.HasValue)
            {
                await _creditCardService.RemoveExpenseAsync(userId, expense.Id, ct);
            }
            else
            {
                await _accountService.SyncMovementAsync(
                    userId, expense.AccountId, FinancialAccountType.Cash, 0,
                    expense.Date, "expense", expense.Id, "Gasto eliminado", ct);
            }
            return true;
        }, cancellationToken);
    }

    public async Task<ExpenseSummaryDto> GetSummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _expenseRepository.GetByUserIdAsync(
            userId,
            page: 1,
            pageSize: int.MaxValue,
            startDate: new DateOnly(year, month, 1),
            endDate: new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
            cancellationToken: cancellationToken);

        var expenseList = items.ToList();
        var totalAmount = expenseList.Sum(e => e.Amount);

        var byCategory = expenseList
            .GroupBy(e => new
            {
                e.CategoryId,
                e.Category.Name,
                e.Category.Color,
                e.Category.Icon
            })
            .Select(g => new ExpenseByCategoryDto
            {
                CategoryName = g.Key.Name,
                CategoryColor = g.Key.Color,
                CategoryIcon = g.Key.Icon,
                Amount = g.Sum(e => e.Amount),
                Percentage = totalAmount > 0
                    ? Math.Round(g.Sum(e => e.Amount) * 100 / totalAmount, 2)
                    : 0
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new ExpenseSummaryDto
        {
            TotalAmount = totalAmount,
            TotalCount = totalCount,
            ByCategory = byCategory
        };
    }

    private async Task<IReadOnlyList<Tag>> ResolveTagsAsync(
        Guid userId, IReadOnlyCollection<Guid>? tagIds, CancellationToken cancellationToken)
    {
        var ids = (tagIds ?? Array.Empty<Guid>()).Distinct().ToArray();
        if (ids.Length > 10)
            throw new DomainException("EXPENSE_TAG_LIMIT", "Un gasto puede tener como maximo 10 etiquetas");
        if (ids.Length == 0)
            return Array.Empty<Tag>();

        var tags = await _tagRepository.GetActiveByIdsAsync(userId, ids, cancellationToken);
        if (tags.Count != ids.Length)
            throw new DomainException("INVALID_EXPENSE_TAGS", "Una o mas etiquetas no existen o no pertenecen al usuario");
        foreach (var tag in tags)
            tag.LastUsedAt = DateTimeOffset.UtcNow;
        return tags;
    }

    /// <summary>
    /// El método describe el instrumento. Una compra liquidada reduce una cuenta
    /// Cash; una compra a crédito aumenta únicamente el pasivo seleccionado.
    /// </summary>
    private async Task<PaymentMethod> ValidatePaymentSourceAsync(
        Guid userId,
        string? paymentMethodValue,
        Guid? accountId,
        Guid? creditCardId,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PaymentMethod>(
                paymentMethodValue?.Replace("_", ""), true, out var paymentMethod))
            throw new DomainException("INVALID_PAYMENT_METHOD", "El método de pago no es válido.");

        if (paymentMethod == PaymentMethod.CreditCard)
        {
            if (accountId.HasValue)
                throw new DomainException("CREDIT_CARD_CASH_ACCOUNT_NOT_ALLOWED",
                    "Una compra con tarjeta de crédito no debe descontar una cuenta de caja.");
            if (!creditCardId.HasValue)
                throw new DomainException("CREDIT_CARD_REQUIRED",
                    "Selecciona la tarjeta de crédito utilizada.");
            await _creditCardService.GetByIdAsync(creditCardId.Value, userId, cancellationToken);
            return paymentMethod;
        }

        if (creditCardId.HasValue)
            throw new DomainException("CREDIT_CARD_NOT_ALLOWED",
                "Solo las compras con método tarjeta de crédito pueden indicar una tarjeta.");
        if (!accountId.HasValue)
            throw new DomainException("EXPENSE_ACCOUNT_REQUIRED",
                "Selecciona explícitamente la cuenta desde la que se pagó el gasto.");

        await _accountService.GetAvailableBalanceAsync(
            userId, accountId, FinancialAccountType.Cash, cancellationToken);
        return paymentMethod;
    }

    private static void EnsureSameExpense(Expense expense, ExpenseCreateDto dto)
    {
        if (!Enum.TryParse<PaymentMethod>(
                dto.PaymentMethod.Replace("_", ""), true, out var paymentMethod)
            || expense.CategoryId != dto.CategoryId
            || expense.AccountId != dto.AccountId
            || expense.CreditCardId != dto.CreditCardId
            || expense.Amount != dto.Amount
            || expense.Date != dto.Date
            || expense.PaymentMethod != paymentMethod)
            throw new DomainException("IDEMPOTENCY_KEY_REUSE",
                "La clave de idempotencia ya fue usada con datos diferentes.");
    }

    /// <summary>
    /// Convierte PascalCase a snake_case.
    /// DebitCard   -> debit_card
    /// CreditCard  -> credit_card
    /// Monthly     -> monthly
    /// </summary>
    private static string ToSnakeCase(string value)
    {
        return string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + c : c.ToString()
        )).ToLower();
    }

    private static ExpenseResponseDto MapToResponseDto(Expense expense) => new()
    {
        Id = expense.Id,
        CategoryId = expense.CategoryId,
        AccountId = expense.AccountId,
        AccountName = expense.Account?.Name,
        CreditCardId = expense.CreditCardId,
        CreditCardName = expense.CreditCard?.Name,
        CategoryName = expense.Category?.Name ?? string.Empty,
        CategoryColor = expense.Category?.Color ?? string.Empty,
        CategoryIcon = expense.Category?.Icon,
        Amount = expense.Amount,
        Description = expense.Description,
        Merchant = expense.Merchant,
        Tags = expense.ExpenseTags
            .Where(et => et.Tag.DeletedAt == null)
            .Select(et => new TagResponseDto
            {
                Id = et.Tag.Id,
                Name = et.Tag.Name,
                Color = et.Tag.Color,
                LastUsedAt = et.Tag.LastUsedAt
            }).OrderBy(t => t.Name).ToList(),
        Date = expense.Date,
        // Convierte PascalCase a snake_case: DebitCard → debit_card
        PaymentMethod = ToSnakeCase(expense.PaymentMethod.ToString()),
        IsRecurring = expense.IsRecurring,
        RecurrenceType = expense.RecurrenceType.HasValue
            ? ToSnakeCase(expense.RecurrenceType.Value.ToString())
            : null,
        Notes = expense.Notes,
        CreatedAt = expense.CreatedAt
    };
}
