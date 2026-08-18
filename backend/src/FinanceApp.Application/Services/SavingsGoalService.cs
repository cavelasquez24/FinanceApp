using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class SavingsGoalService : ISavingsGoalService
{
    private readonly ISavingsGoalRepository _savingsGoalRepository;
    private readonly IFinancialAccountService _accountService;
    private readonly IUnitOfWork? _unitOfWork;

    public SavingsGoalService(ISavingsGoalRepository savingsGoalRepository, IFinancialAccountService accountService, IUnitOfWork? unitOfWork = null)
    {
        _savingsGoalRepository = savingsGoalRepository;
        _accountService = accountService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SavingsGoalResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
        => (await _savingsGoalRepository.GetByUserIdAsync(userId, cancellationToken)).Select(MapToResponseDto);

    public async Task<SavingsGoalResponseDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => MapToResponseDto(await GetActiveGoalAsync(id, userId, cancellationToken));

    public async Task<SavingsGoalResponseDto> CreateAsync(Guid userId, SavingsGoalCreateDto dto, CancellationToken cancellationToken = default)
    {
        return await RunInTransactionAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.TargetAmount <= 0 || dto.InitialAmount < 0)
                throw new DomainException("INVALID_GOAL_AMOUNT", "Nombre, objetivo y monto inicial de la meta no son válidos.");
            if (dto.InitialAmount > dto.TargetAmount)
                throw new DomainException("INITIAL_AMOUNT_EXCEEDS_TARGET", "El monto inicial no puede superar el objetivo de la meta.");
            if (dto.InitialAmount > 0 && dto.IdempotencyKey.HasValue && dto.IdempotencyKey != Guid.Empty)
            {
                var previous = (await _savingsGoalRepository.GetByUserIdAsync(userId, ct))
                    .SingleOrDefault(g => g.Contributions.Any(c => c.OperationId == dto.IdempotencyKey));
                if (previous != null)
                    return MapToResponseDto(previous);
            }
            if (!Enum.TryParse<SavingsGoalPurpose>(dto.Purpose.Replace("_", ""), true, out var purpose))
                throw new DomainException("INVALID_GOAL_PURPOSE", "El propósito de la meta no es válido.");
            if (dto.MinimumProtectedAmount is < 0 || dto.MinimumProtectedAmount > dto.TargetAmount)
                throw new DomainException("INVALID_PROTECTED_AMOUNT", "El mínimo protegido debe estar entre cero y el objetivo.");
            if (dto.InitialAmount > 0 && (!dto.InitialSourceAccountId.HasValue || dto.InitialSourceAccountId == Guid.Empty || !dto.IdempotencyKey.HasValue || dto.IdempotencyKey == Guid.Empty))
                throw new DomainException("INITIAL_FUNDING_REQUIRED", "El saldo inicial requiere cuenta de origen y clave de idempotencia.");

            if (purpose == SavingsGoalPurpose.EmergencyFund)
            {
                var goals = await _savingsGoalRepository.GetByUserIdAsync(userId, ct);
                if (goals.Any(g => g.Purpose == SavingsGoalPurpose.EmergencyFund))
                    throw new DomainException("EMERGENCY_FUND_ALREADY_EXISTS", "Solo puede existir un fondo de emergencia activo.");
            }

            if (dto.InitialAmount > 0)
            {
                var source = await EnsureLiquidAccountAsync(userId, dto.InitialSourceAccountId!.Value, ct);
                if (source.CurrentBalance < dto.InitialAmount)
                    throw new DomainException("INSUFFICIENT_ACCOUNT_BALANCE", "La cuenta de respaldo no tiene saldo suficiente.");
                await EnsureAllocationBackedAsync(userId, dto.InitialAmount, ct);
            }
            var goal = new SavingsGoal
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                TargetAmount = dto.TargetAmount,
                CurrentAmount = dto.InitialAmount,
                TargetDate = dto.TargetDate,
                Icon = dto.Icon?.Trim(),
                Purpose = purpose,
                MinimumProtectedAmount = purpose == SavingsGoalPurpose.EmergencyFund ? dto.MinimumProtectedAmount : null,
                IsCompleted = dto.InitialAmount == dto.TargetAmount
            };
            await _savingsGoalRepository.CreateAsync(goal, ct);

            if (dto.InitialAmount > 0)
            {

                var operationId = dto.IdempotencyKey!.Value;
                var date = dto.InitialFundingDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var contribution = new SavingsGoalContribution
                {
                    SavingsGoalId = goal.Id,
                    ContributionDate = date,
                    Amount = dto.InitialAmount,
                    Notes = "Saldo inicial financiado",
                    OperationId = operationId
                };
                await _savingsGoalRepository.AddContributionAsync(contribution, ct);

            }
            return MapToResponseDto(goal);
        }, cancellationToken);
    }

    public async Task<SavingsGoalResponseDto> UpdateAsync(Guid id, Guid userId, SavingsGoalUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var goal = await GetActiveGoalAsync(id, userId, cancellationToken);
        if (!Enum.TryParse<SavingsGoalPurpose>(dto.Purpose.Replace("_", ""), true, out var purpose))
            throw new DomainException("INVALID_GOAL_PURPOSE", "El propósito de la meta no es válido.");
        if (dto.TargetAmount <= 0 || dto.TargetAmount < goal.CurrentAmount || dto.MinimumProtectedAmount is < 0 || dto.MinimumProtectedAmount > dto.TargetAmount)
            throw new DomainException("INVALID_GOAL_AMOUNT", "El objetivo no puede ser menor que el saldo y el mínimo protegido debe ser válido.");
        if (purpose == SavingsGoalPurpose.EmergencyFund && goal.Purpose != SavingsGoalPurpose.EmergencyFund)
        {
            var goals = await _savingsGoalRepository.GetByUserIdAsync(userId, cancellationToken);
            if (goals.Any(g => g.Id != id && g.Purpose == SavingsGoalPurpose.EmergencyFund))
                throw new DomainException("EMERGENCY_FUND_ALREADY_EXISTS", "Solo puede existir un fondo de emergencia activo.");
        }
        if (goal.Purpose == SavingsGoalPurpose.EmergencyFund && purpose != SavingsGoalPurpose.EmergencyFund && HasOpenRestorations(goal))
            throw new DomainException("OPEN_RESTORATIONS", "No se puede cambiar el propósito mientras existan restauraciones abiertas.");

        goal.Name = dto.Name.Trim(); goal.Description = dto.Description?.Trim(); goal.TargetAmount = dto.TargetAmount;
        goal.TargetDate = dto.TargetDate; goal.Icon = dto.Icon?.Trim(); goal.Purpose = purpose;
        goal.MinimumProtectedAmount = purpose == SavingsGoalPurpose.EmergencyFund ? dto.MinimumProtectedAmount : null;
        goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount;
        await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);
        return MapToResponseDto(goal);
    }

    public Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => DeleteAsync(id, userId, null, cancellationToken);

    public async Task DeleteAsync(Guid id, Guid userId, SavingsGoalArchiveDto? dto, CancellationToken cancellationToken = default)
    {
        await RunInTransactionAsync(async ct =>
        {
            var goal = await GetActiveGoalAsync(id, userId, ct);
            if (HasOpenRestorations(goal))
                throw new DomainException("OPEN_RESTORATIONS", "No se puede archivar un fondo con restauraciones abiertas.");
            if (goal.CurrentAmount == 0)
            {
                goal.DeletedAt = DateTimeOffset.UtcNow;
                await _savingsGoalRepository.UpdateAsync(goal, ct);
                return true;
            }
            if (dto == null || dto.IdempotencyKey == Guid.Empty || string.IsNullOrWhiteSpace(dto.Resolution))
                throw new DomainException("GOAL_ARCHIVE_RESOLUTION_REQUIRED", "Elige liberar el saldo o reasignarlo antes de archivar.");

            var amount = goal.CurrentAmount;
            var date = dto.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (string.Equals(dto.Resolution, "release", StringComparison.OrdinalIgnoreCase))
            {
                if (!dto.DestinationAccountId.HasValue || dto.DestinationAccountId == Guid.Empty)
                    throw new DomainException("DESTINATION_ACCOUNT_REQUIRED", "Selecciona la cuenta líquida que recibirá el saldo.");
                await EnsureLiquidAccountAsync(userId, dto.DestinationAccountId.Value, ct);
                await AddWithdrawalAsync(goal, amount, date, SavingsWithdrawalReason.ReallocatedToLiquid, dto.IdempotencyKey, dto.Notes, ct);
            }
            else if (string.Equals(dto.Resolution, "reassign", StringComparison.OrdinalIgnoreCase))
            {
                var target = await GetArchiveTargetAsync(goal, dto.TargetGoalId, userId, ct);
                EnsureCapacity(target, amount);
                await AddWithdrawalAsync(goal, amount, date, SavingsWithdrawalReason.ReallocatedToOtherGoal, dto.IdempotencyKey, dto.Notes, ct);
                target.CurrentAmount += amount; target.IsCompleted = target.CurrentAmount >= target.TargetAmount;
                await _savingsGoalRepository.AddContributionAsync(new SavingsGoalContribution { SavingsGoalId = target.Id, ContributionDate = date, Amount = amount, Notes = $"Reasignación desde {goal.Name}", OperationId = dto.IdempotencyKey }, ct);
                await _savingsGoalRepository.UpdateAsync(target, ct);
            }
            else
                throw new DomainException("INVALID_ARCHIVE_RESOLUTION", "La acción de archivo no es válida.");

            goal.CurrentAmount = 0; goal.IsCompleted = false; goal.DeletedAt = DateTimeOffset.UtcNow;
            await _savingsGoalRepository.UpdateAsync(goal, ct);
            return true;
        }, cancellationToken);
    }

    public async Task<SavingsGoalResponseDto> DepositAsync(Guid id, Guid userId, DepositDto dto, CancellationToken cancellationToken = default)
    {
        return await RunInTransactionAsync(async ct =>
        {
            var goal = await GetActiveGoalAsync(id, userId, ct);
            if (HasOpenRestorations(goal)) throw new DomainException("USE_RESTORATION_PAYMENT", "Registra el aporte contra una restauración abierta.");
            if (dto.Amount <= 0) throw new DomainException("INVALID_DEPOSIT_AMOUNT", "El monto del aporte debe ser mayor a cero.");
            if (dto.SourceAccountId == Guid.Empty || dto.IdempotencyKey == Guid.Empty) throw new DomainException("SOURCE_AND_IDEMPOTENCY_REQUIRED", "El aporte requiere cuenta de origen y clave de idempotencia.");
            if (goal.Contributions.Any(c => c.OperationId == dto.IdempotencyKey)) return MapToResponseDto(goal);
            EnsureCapacity(goal, dto.Amount);
            var source = await EnsureLiquidAccountAsync(userId, dto.SourceAccountId, ct);
            if (source.CurrentBalance < dto.Amount)
                throw new DomainException("INSUFFICIENT_ACCOUNT_BALANCE", "La cuenta de respaldo no tiene saldo suficiente.");
            await EnsureAllocationBackedAsync(userId, dto.Amount, ct);
            var date = dto.ContributionDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            goal.CurrentAmount += dto.Amount; goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount;
            await _savingsGoalRepository.AddContributionAsync(new SavingsGoalContribution { SavingsGoalId = goal.Id, ContributionDate = date, Amount = dto.Amount, Notes = dto.Notes, OperationId = dto.IdempotencyKey }, ct);

            await _savingsGoalRepository.UpdateAsync(goal, ct);
            return MapToResponseDto(goal);
        }, cancellationToken);
    }

    public async Task<SavingsGoalWithdrawalResponseDto> WithdrawAsync(Guid id, Guid userId, SavingsGoalWithdrawalCreateDto dto, CancellationToken cancellationToken = default)
    {
        return await RunInTransactionAsync(async ct =>
        {
            var goal = await GetActiveGoalAsync(id, userId, ct);
            if (dto.Amount <= 0 || dto.Amount > goal.CurrentAmount) throw new DomainException("INSUFFICIENT_SAVINGS_BALANCE", "El retiro debe ser positivo y no puede superar el saldo disponible.");
            if (dto.IdempotencyKey == Guid.Empty) throw new DomainException("INVALID_IDEMPOTENCY_KEY", "La clave de idempotencia es obligatoria.");
            var existing = goal.Withdrawals.SingleOrDefault(w => w.OperationId == dto.IdempotencyKey);
            if (existing != null) return MapWithdrawal(existing, goal.CurrentAmount);
            if (dto.LinkedExpenseId.HasValue && dto.Reason != SavingsWithdrawalReason.Consumed) throw new DomainException("INVALID_LINKED_EXPENSE", "LinkedExpenseId solo es válido para consumo.");
            if (dto.Reason == SavingsWithdrawalReason.Correction && string.IsNullOrWhiteSpace(dto.Notes))
                throw new DomainException("CORRECTION_REASON_REQUIRED", "Explica el motivo de la corrección para conservar trazabilidad.");
            if (dto.Reason != SavingsWithdrawalReason.Correction && goal.Purpose == SavingsGoalPurpose.EmergencyFund && goal.CurrentAmount - dto.Amount < (goal.MinimumProtectedAmount ?? 0)) throw new DomainException("MINIMUM_PROTECTED_AMOUNT", "El retiro dejaría el fondo bajo su mínimo protegido.");

            var date = dto.WithdrawalDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (dto.Reason is SavingsWithdrawalReason.ReallocatedToOtherGoal)
            {
                var target = await GetArchiveTargetAsync(goal, dto.TargetGoalId, userId, ct);
                EnsureCapacity(target, dto.Amount);
                var withdrawal = await AddWithdrawalAsync(goal, dto.Amount, date, dto.Reason, dto.IdempotencyKey, dto.Notes, ct);
                target.CurrentAmount += dto.Amount; target.IsCompleted = target.CurrentAmount >= target.TargetAmount;
                await _savingsGoalRepository.AddContributionAsync(new SavingsGoalContribution { SavingsGoalId = target.Id, ContributionDate = date, Amount = dto.Amount, Notes = $"Reasignación desde {goal.Name}", OperationId = dto.IdempotencyKey }, ct);
                await _savingsGoalRepository.UpdateAsync(target, ct);
                return MapWithdrawal(withdrawal, goal.CurrentAmount);
            }

            var withdrawalCreated = await AddWithdrawalAsync(goal, dto.Amount, date, dto.Reason, dto.IdempotencyKey, dto.Notes, ct, dto.LinkedExpenseId);
            if (dto.Reason != SavingsWithdrawalReason.Correction)
            {
                if (!dto.DestinationAccountId.HasValue || dto.DestinationAccountId == Guid.Empty) throw new DomainException("DESTINATION_ACCOUNT_REQUIRED", "Selecciona la cuenta líquida de destino.");
                await EnsureLiquidAccountAsync(userId, dto.DestinationAccountId.Value, ct);

            }
            return MapWithdrawal(withdrawalCreated, goal.CurrentAmount);
        }, cancellationToken);
    }

    private async Task<SavingsGoal> GetActiveGoalAsync(Guid id, Guid userId, CancellationToken ct)
        => await _savingsGoalRepository.GetByIdWithHistoryAsync(id, userId, ct) ?? throw new NotFoundException("Meta de ahorro", id);

    private async Task<SavingsGoal> GetArchiveTargetAsync(SavingsGoal source, Guid? targetId, Guid userId, CancellationToken ct)
    {
        if (!targetId.HasValue || targetId == Guid.Empty || targetId == source.Id) throw new DomainException("TARGET_GOAL_REQUIRED", "Selecciona otra meta activa como destino.");
        return await GetActiveGoalAsync(targetId.Value, userId, ct);
    }

    private static void EnsureCapacity(SavingsGoal goal, decimal amount)
    {
        if (goal.CurrentAmount + amount > goal.TargetAmount) throw new DomainException("GOAL_TARGET_EXCEEDED", "La operación supera el objetivo de la meta; ajusta el monto o el objetivo primero.");
    }

    private async Task<SavingsGoalWithdrawal> AddWithdrawalAsync(SavingsGoal goal, decimal amount, DateOnly date, SavingsWithdrawalReason reason, Guid operationId, string? notes, CancellationToken ct, Guid? linkedExpenseId = null)
    {
        goal.CurrentAmount -= amount; goal.IsCompleted = false;
        var withdrawal = new SavingsGoalWithdrawal { SavingsGoalId = goal.Id, WithdrawalDate = date, Amount = amount, Reason = reason, Notes = notes, OperationId = operationId, LinkedExpenseId = reason == SavingsWithdrawalReason.Consumed ? linkedExpenseId : null };
        await _savingsGoalRepository.AddWithdrawalAsync(withdrawal, ct);
        await _savingsGoalRepository.UpdateAsync(goal, ct);
        return withdrawal;
    }


    private Task<T> RunInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        => _unitOfWork == null ? action(cancellationToken) : _unitOfWork.ExecuteInTransactionAsync(action, cancellationToken);

    private async Task<FinancialAccountResponseDto> EnsureLiquidAccountAsync(Guid userId, Guid accountId, CancellationToken cancellationToken)
    {
        var account = (await _accountService.GetAllAsync(userId, cancellationToken)).SingleOrDefault(a => a.Id == accountId);
        if (account == null) throw new NotFoundException("Cuenta", accountId);
        if (!account.IsActive) throw new DomainException("INACTIVE_ACCOUNT", "La cuenta seleccionada está inactiva.");
        if (!string.Equals(account.Type, "cash", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(account.Type, "savings", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("INVALID_LIQUID_ACCOUNT", "Selecciona una cuenta líquida para esta operación.");
        return account;
    }

    private async Task EnsureAllocationBackedAsync(Guid userId, decimal additionalAmount, CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAllAsync(userId, cancellationToken);
        var liquidBalance = accounts
            .Where(a => a.IsActive && (string.Equals(a.Type, "cash", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a.Type, "savings", StringComparison.OrdinalIgnoreCase)))
            .Sum(a => a.CurrentBalance);
        var allocated = (await _savingsGoalRepository.GetByUserIdAsync(userId, cancellationToken))
            .Sum(g => g.CurrentAmount);
        if (allocated + additionalAmount > liquidBalance)
            throw new DomainException(
                "INSUFFICIENT_UNALLOCATED_SAVINGS",
                "No hay saldo líquido sin asignar suficiente para respaldar esta meta.");
    }

    private static bool HasOpenRestorations(SavingsGoal goal) => goal.Restorations.Any(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted);

    private static SavingsGoalWithdrawalResponseDto MapWithdrawal(SavingsGoalWithdrawal withdrawal, decimal amountAfter) => new()
    {
        Id = withdrawal.Id, WithdrawalDate = withdrawal.WithdrawalDate, Amount = withdrawal.Amount, LinkedExpenseId = withdrawal.LinkedExpenseId, Reason = withdrawal.Reason, Notes = withdrawal.Notes, CreatedAt = withdrawal.CreatedAt, GoalCurrentAmountAfter = amountAfter
    };

    private static SavingsGoalResponseDto MapToResponseDto(SavingsGoal goal) => new()
    {
        Id = goal.Id, Name = goal.Name, Description = goal.Description, TargetAmount = goal.TargetAmount, CurrentAmount = goal.CurrentAmount, RemainingAmount = goal.RemainingAmount, ProgressPercentage = goal.ProgressPercentage, TargetDate = goal.TargetDate, IsCompleted = goal.IsCompleted, Icon = goal.Icon, EstimatedMonthsToComplete = null, CreatedAt = goal.CreatedAt,
        Purpose = goal.Purpose == SavingsGoalPurpose.EmergencyFund ? "emergency_fund" : "general", MinimumProtectedAmount = goal.MinimumProtectedAmount,
        PendingRestorationAmount = goal.Restorations.Where(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted).Sum(r => r.OutstandingAmount), OpenRestorationsCount = goal.Restorations.Count(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted), NextRestorationDate = goal.Restorations.Where(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted).Select(r => (DateOnly?)r.NextScheduledDate).Min()
    };
}