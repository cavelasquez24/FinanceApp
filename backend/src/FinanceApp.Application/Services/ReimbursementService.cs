using FinanceApp.Application.DTOs.Reimbursement;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class ReimbursementService : IReimbursementService
{
    private const string SourceType = "reimbursement";
    private readonly IReimbursementRepository _repository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IFinancialAccountService _accountService;
    private readonly ICreditCardService _creditCardService;
    private readonly IUnitOfWork _unitOfWork;

    public ReimbursementService(IReimbursementRepository repository, IExpenseRepository expenseRepository,
        IFinancialAccountService accountService, ICreditCardService creditCardService, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _expenseRepository = expenseRepository;
        _accountService = accountService;
        _creditCardService = creditCardService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ReimbursementResponseDto>> GetAllAsync(
        Guid userId, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default) =>
        (await _repository.GetByUserIdAsync(userId, startDate, endDate, cancellationToken)).Select(Map).ToList();

    public async Task<ReimbursementResponseDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Map(await GetOwnedAsync(id, userId, cancellationToken));

    public async Task<ReimbursementResponseDto> CreateAsync(
        Guid userId, ReimbursementCreateDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        var existing = await _repository.GetByIdempotencyKeyAsync(userId, dto.IdempotencyKey, cancellationToken);
        if (existing != null)
        {
            EnsureSame(existing, dto);
            return Map(existing);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await ValidateLinkedExpenseAsync(userId, dto.ExpenseId, dto.Amount, null, ct);
            var item = FromDto(userId, dto);
            await _repository.CreateAsync(item, ct);
            await SyncDestinationAsync(item, ct);
            return await GetByIdAsync(item.Id, userId, ct);
        }, cancellationToken);
    }

    public async Task<ReimbursementResponseDto> UpdateAsync(
        Guid id, Guid userId, ReimbursementUpdateDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var item = await GetOwnedAsync(id, userId, ct);
            if (dto.IdempotencyKey != item.IdempotencyKey)
                throw new DomainException("IMMUTABLE_IDEMPOTENCY_KEY", "La clave de idempotencia no se puede modificar.");

            await ValidateLinkedExpenseAsync(userId, dto.ExpenseId, dto.Amount, item.Id, ct);
            var oldType = item.DestinationType;
            var oldAccountId = item.AccountId;
            var oldCardId = item.CreditCardId;
            ApplyDto(item, dto);

            if (oldType != item.DestinationType
                || oldAccountId != item.AccountId || oldCardId != item.CreditCardId)
                await RemoveDestinationAsync(item.Id, userId, oldType, oldAccountId, oldCardId, ct);

            await _repository.UpdateAsync(item, ct);
            await SyncDestinationAsync(item, ct);
            return await GetByIdAsync(item.Id, userId, ct);
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var item = await GetOwnedAsync(id, userId, ct);
            await RemoveDestinationAsync(item.Id, userId, item.DestinationType, item.AccountId, item.CreditCardId, ct);
            item.DeletedAt = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(item, ct);
            return true;
        }, cancellationToken);
    }

    public async Task<ReimbursementSummaryDto> GetSummaryAsync(
        Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var gross = await _expenseRepository.GetTotalByDateRangeAsync(userId, startDate, endDate, cancellationToken);
        var refunds = await _repository.GetTotalByDateRangeAsync(userId, startDate, endDate, cancellationToken);
        return new ReimbursementSummaryDto
        {
            GrossExpenses = gross,
            ReimbursementsReceived = refunds,
            NetPersonalExpenses = gross - refunds
        };
    }

    private async Task SyncDestinationAsync(Reimbursement item, CancellationToken cancellationToken)
    {
        var description = item.Expense?.Description is { Length: > 0 } expenseDescription
            ? $"Reembolso: {expenseDescription}" : "Reembolso recibido";
        if (item.DestinationType == ReimbursementDestinationType.Account)
        {
            await _accountService.SyncMovementAsync(item.UserId, item.AccountId,
                FinancialAccountType.Cash, item.Amount, item.Date, SourceType, item.Id,
                description, cancellationToken);
            return;
        }

        await _creditCardService.SyncReimbursementAsync(item.UserId, item.CreditCardId!.Value,
            item.Id, item.Amount, item.Date, description, cancellationToken);
    }

    private async Task RemoveDestinationAsync(Guid id, Guid userId,
        ReimbursementDestinationType destinationType, Guid? accountId, Guid? creditCardId,
        CancellationToken cancellationToken)
    {
        if (destinationType == ReimbursementDestinationType.Account)
        {
            await _accountService.SyncMovementAsync(userId, accountId, FinancialAccountType.Cash,
                0m, DateOnly.FromDateTime(DateTime.Today), SourceType, id,
                "Reembolso anulado", cancellationToken);
            return;
        }

        if (creditCardId.HasValue)
            await _creditCardService.SyncReimbursementAsync(userId, creditCardId.Value,
                id, 0m, DateOnly.FromDateTime(DateTime.Today), "Reembolso anulado", cancellationToken);
    }

    private async Task ValidateLinkedExpenseAsync(Guid userId, Guid? expenseId, decimal amount,
        Guid? excludingId, CancellationToken cancellationToken)
    {
        if (!expenseId.HasValue) return;
        var expense = await _expenseRepository.GetByIdAsync(expenseId.Value, cancellationToken);
        if (expense == null || expense.UserId != userId || expense.IsDeleted)
            throw new DomainException("INVALID_REIMBURSEMENT_EXPENSE", "El gasto relacionado no existe o no pertenece al usuario.");

        var reimbursed = await _repository.GetTotalByExpenseIdAsync(userId, expense.Id, excludingId, cancellationToken);
        if (reimbursed + amount > expense.Amount)
            throw new DomainException("REIMBURSEMENT_EXCEEDS_EXPENSE",
                "Los reembolsos vinculados no pueden superar el monto del gasto.");
    }

    private async Task<Reimbursement> GetOwnedAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        await _repository.GetByIdWithDetailsAsync(id, userId, cancellationToken)
            ?? throw new NotFoundException("Reembolso", id);

    private static void Validate(ReimbursementCreateDto dto)
    {
        var destination = ParseDestination(dto.DestinationType);
        if (dto.Amount <= 0) throw new DomainException("INVALID_REIMBURSEMENT_AMOUNT", "El monto debe ser mayor que cero.");
        if (dto.Date == default || dto.Date > DateOnly.FromDateTime(DateTime.Today))
            throw new DomainException("INVALID_REIMBURSEMENT_DATE", "La fecha del reembolso es obligatoria y no puede ser futura.");
        if (dto.IdempotencyKey == Guid.Empty)
            throw new DomainException("INVALID_IDEMPOTENCY_KEY", "La clave de idempotencia es obligatoria.");
        if (dto.Person?.Trim().Length > 160 || dto.Notes?.Trim().Length > 1000)
            throw new DomainException("INVALID_REIMBURSEMENT_METADATA", "La persona o nota excede la longitud permitida.");

        var accountDestination = destination == ReimbursementDestinationType.Account;
        if (accountDestination != dto.AccountId.HasValue || !accountDestination != dto.CreditCardId.HasValue)
            throw new DomainException("INVALID_REIMBURSEMENT_DESTINATION",
                "Selecciona exactamente una cuenta receptora o una tarjeta.");
    }

    private static Reimbursement FromDto(Guid userId, ReimbursementCreateDto dto)
    {
        var item = new Reimbursement { Id = Guid.NewGuid(), UserId = userId, IdempotencyKey = dto.IdempotencyKey };
        ApplyDto(item, dto);
        return item;
    }

    private static void ApplyDto(Reimbursement item, ReimbursementCreateDto dto)
    {
        item.ExpenseId = dto.ExpenseId;
        item.DestinationType = ParseDestination(dto.DestinationType);
        item.AccountId = item.DestinationType == ReimbursementDestinationType.Account ? dto.AccountId : null;
        item.CreditCardId = item.DestinationType == ReimbursementDestinationType.CreditCard ? dto.CreditCardId : null;
        item.Amount = dto.Amount;
        item.Date = dto.Date;
        item.Person = dto.Person?.Trim();
        item.Notes = dto.Notes?.Trim();
    }

    private static void EnsureSame(Reimbursement item, ReimbursementCreateDto dto)
    {
        var destination = ParseDestination(dto.DestinationType);
        if (item.ExpenseId != dto.ExpenseId || item.DestinationType != destination
            || item.AccountId != dto.AccountId || item.CreditCardId != dto.CreditCardId
            || item.Amount != dto.Amount || item.Date != dto.Date)
            throw new DomainException("IDEMPOTENCY_KEY_REUSE", "La clave de idempotencia ya fue usada con datos diferentes.");
    }

    private static ReimbursementDestinationType ParseDestination(string? destinationType) =>
        destinationType?.Trim().ToLowerInvariant() switch
        {
            "account" => ReimbursementDestinationType.Account,
            "credit_card" or "creditcard" => ReimbursementDestinationType.CreditCard,
            _ => throw new DomainException(
                "INVALID_REIMBURSEMENT_DESTINATION",
                "El destino debe ser una cuenta o una tarjeta.")
        };
    private static ReimbursementResponseDto Map(Reimbursement item) => new()
    {
        Id = item.Id,
        ExpenseId = item.ExpenseId,
        ExpenseDescription = item.Expense?.Description,
        DestinationType = item.DestinationType == ReimbursementDestinationType.Account ? "account" : "credit_card",
        AccountId = item.AccountId,
        AccountName = item.Account?.Name,
        CreditCardId = item.CreditCardId,
        CreditCardName = item.CreditCard?.Name,
        Amount = item.Amount,
        Date = item.Date,
        Person = item.Person,
        Notes = item.Notes,
        IdempotencyKey = item.IdempotencyKey,
        CreatedAt = item.CreatedAt
    };
}
