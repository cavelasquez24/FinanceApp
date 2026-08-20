using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.Expense;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class SavingsGoalService : ISavingsGoalService
{
    private const string ExistingBalanceMode = "existing_balance";
    private const string AccountTransferMode = "account_transfer";
    private readonly ISavingsGoalRepository _savingsGoalRepository;
    private readonly IFinancialAccountService _accountService;
    private readonly IExpenseService _expenseService;
    private readonly IUnitOfWork? _unitOfWork;

    public SavingsGoalService(ISavingsGoalRepository savingsGoalRepository, IFinancialAccountService accountService, IExpenseService expenseService, IUnitOfWork? unitOfWork = null)
    {
        _savingsGoalRepository = savingsGoalRepository;
        _accountService = accountService;
        _expenseService = expenseService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SavingsGoalResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var goals = (await _savingsGoalRepository.GetByUserIdAsync(userId, cancellationToken)).ToList();
        var accounts = await _accountService.GetAllAsync(userId, cancellationToken);
        var defaultSavings = accounts.FirstOrDefault(account => account.IsActive && account.IsDefault
            && string.Equals(account.Type, "savings", StringComparison.OrdinalIgnoreCase));
        foreach (var goal in goals.Where(goal => !goal.SavingsAccountId.HasValue))
        {
            if (defaultSavings == null) continue;
            goal.SavingsAccountId = defaultSavings.Id;
            await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);
        }
        return goals.Select(goal => MapToResponseDto(
            goal, accounts.SingleOrDefault(account => account.Id == goal.SavingsAccountId)));
    }

    public async Task<SavingsGoalResponseDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var goal = await GetActiveGoalAsync(id, userId, cancellationToken);
        var account = await EnsureGoalSavingsAccountAsync(goal, userId, cancellationToken);
        return MapToResponseDto(goal, account);
    }

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
                {
                    var previousAccount = await EnsureGoalSavingsAccountAsync(previous, userId, ct);
                    return MapToResponseDto(previous, previousAccount);
                }
            }
            if (!Enum.TryParse<SavingsGoalPurpose>(dto.Purpose.Replace("_", ""), true, out var purpose))
                throw new DomainException("INVALID_GOAL_PURPOSE", "El propósito de la meta no es válido.");
            if (dto.MinimumProtectedAmount is < 0 || dto.MinimumProtectedAmount > dto.TargetAmount)
                throw new DomainException("INVALID_PROTECTED_AMOUNT", "El mínimo protegido debe estar entre cero y el objetivo.");
            if (dto.SavingsAccountId == Guid.Empty)
                throw new DomainException("SAVINGS_ACCOUNT_REQUIRED", "Selecciona la cuenta de ahorro que respalda esta meta.");
            if (dto.InitialAmount > 0 && (!dto.IdempotencyKey.HasValue || dto.IdempotencyKey == Guid.Empty))
                throw new DomainException("INITIAL_OPERATION_REQUIRED", "El saldo inicial requiere una clave de idempotencia.");

            if (purpose == SavingsGoalPurpose.EmergencyFund)
            {
                var goals = await _savingsGoalRepository.GetByUserIdAsync(userId, ct);
                if (goals.Any(g => g.Purpose == SavingsGoalPurpose.EmergencyFund))
                    throw new DomainException("EMERGENCY_FUND_ALREADY_EXISTS", "Solo puede existir un fondo de emergencia activo.");
            }

            var savingsAccount = await EnsureSavingsAccountAsync(userId, dto.SavingsAccountId, ct);
            Guid? sourceAccountId = null;
            if (dto.InitialAmount > 0)
            {
                sourceAccountId = await ValidateFundingAsync(
                    userId, savingsAccount, dto.InitialAmount,
                    dto.InitialFundingMode, dto.InitialSourceAccountId, ct);
            }


            var goal = new SavingsGoal
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Id = Guid.NewGuid(),
                SavingsAccountId = savingsAccount.Id,
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
                    Id = Guid.NewGuid(),
                    SavingsGoalId = goal.Id,
                    ContributionDate = date,
                    Amount = dto.InitialAmount,
                    Notes = sourceAccountId.HasValue ? "Saldo inicial transferido a la cuenta de ahorro" : "Saldo inicial asignado desde ahorro disponible",
                    OperationId = operationId,
                    SourceAccountId = sourceAccountId
                };
                await _savingsGoalRepository.AddContributionAsync(contribution, ct);

                if (sourceAccountId.HasValue)
                {
                    await _accountService.SyncTransferBetweenAccountsAsync(
                        userId,
                        sourceAccountId,
                        FinancialAccountType.Cash,
                        savingsAccount.Id,
                        FinancialAccountType.Savings,
                        dto.InitialAmount,
                        date,
                        "savings-goal-funding",
                        contribution.Id,
                        $"Aporte inicial: {goal.Name}",
                        ct);
                }
            }
            return MapToResponseDto(goal, await EnsureSavingsAccountAsync(userId, savingsAccount.Id, ct));
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
        var account = await EnsureGoalSavingsAccountAsync(goal, userId, cancellationToken);
        return MapToResponseDto(goal, account);
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
            var savingsAccount = await EnsureGoalSavingsAccountAsync(goal, userId, ct);
            if (HasOpenRestorations(goal)) throw new DomainException("USE_RESTORATION_PAYMENT", "Registra el aporte contra una restauración abierta.");
            if (dto.Amount <= 0) throw new DomainException("INVALID_DEPOSIT_AMOUNT", "El monto del aporte debe ser mayor a cero.");
            if (dto.IdempotencyKey == Guid.Empty) throw new DomainException("IDEMPOTENCY_REQUIRED", "El aporte requiere una clave de idempotencia.");
            if (goal.Contributions.Any(c => c.OperationId == dto.IdempotencyKey)) return MapToResponseDto(goal, savingsAccount);
            EnsureCapacity(goal, dto.Amount);
            var sourceAccountId = await ValidateFundingAsync(
                userId, savingsAccount, dto.Amount, dto.FundingMode, dto.SourceAccountId, ct);

            var date = dto.ContributionDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            goal.CurrentAmount += dto.Amount; goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount;
            var contribution = new SavingsGoalContribution
            {
                Id = Guid.NewGuid(),
                SavingsGoalId = goal.Id,
                ContributionDate = date,
                Amount = dto.Amount,
                Notes = dto.Notes?.Trim(),
                OperationId = dto.IdempotencyKey,
                SourceAccountId = sourceAccountId
            };
            await _savingsGoalRepository.AddContributionAsync(contribution, ct);
            if (sourceAccountId.HasValue)
            {
                await _accountService.SyncTransferBetweenAccountsAsync(
                    userId,
                    sourceAccountId,
                    FinancialAccountType.Cash,
                    savingsAccount.Id,
                    FinancialAccountType.Savings,
                    dto.Amount,
                    date,
                    "savings-goal-funding",
                    contribution.Id,
                    $"Aporte a meta: {goal.Name}",
                    ct);
            }

            await _savingsGoalRepository.UpdateAsync(goal, ct);
            return MapToResponseDto(goal, await EnsureSavingsAccountAsync(userId, savingsAccount.Id, ct));
        }, cancellationToken);
    }

    public async Task<SavingsGoalWithdrawalResponseDto> WithdrawAsync(Guid id, Guid userId, SavingsGoalWithdrawalCreateDto dto, CancellationToken cancellationToken = default)
    {
        return await RunInTransactionAsync(async ct =>
        {
            var goal = await GetActiveGoalAsync(id, userId, ct);
            var savingsAccount = await EnsureGoalSavingsAccountAsync(goal, userId, ct);
            if (dto.Amount <= 0 || dto.Amount > goal.CurrentAmount) throw new DomainException("INSUFFICIENT_SAVINGS_BALANCE", "El retiro debe ser positivo y no puede superar el saldo disponible.");
            if (dto.IdempotencyKey == Guid.Empty) throw new DomainException("INVALID_IDEMPOTENCY_KEY", "La clave de idempotencia es obligatoria.");
            var existing = goal.Withdrawals.SingleOrDefault(w => w.OperationId == dto.IdempotencyKey);
            if (existing != null) return MapWithdrawal(existing, goal.CurrentAmount);
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

            if (dto.Reason == SavingsWithdrawalReason.Consumed)
            {
                if (!dto.ExpenseCategoryId.HasValue || string.IsNullOrWhiteSpace(dto.ExpenseDescription))
                    throw new DomainException("EXPENSE_DETAILS_REQUIRED", "Selecciona una categoría y describe el gasto realizado con el ahorro.");
                if (savingsAccount.CurrentBalance < dto.Amount)
                    throw new DomainException("INSUFFICIENT_SAVINGS_ACCOUNT_BALANCE", "La cuenta de ahorro no tiene saldo real suficiente.");

                var consumedWithdrawal = await AddWithdrawalAsync(
                    goal, dto.Amount, date, dto.Reason, dto.IdempotencyKey, dto.Notes, ct);
                var expense = await _expenseService.CreateAsync(userId, new ExpenseCreateDto
                {
                    CategoryId = dto.ExpenseCategoryId.Value,
                    AccountId = savingsAccount.Id,
                    IdempotencyKey = dto.IdempotencyKey,
                    Amount = dto.Amount,
                    Description = dto.ExpenseDescription.Trim(),
                    Date = date,
                    PaymentMethod = "cash",
                    IsRecurring = false,
                    Notes = dto.Notes?.Trim()
                }, ct);
                consumedWithdrawal.LinkedExpenseId = expense.Id;
                await _savingsGoalRepository.UpdateAsync(goal, ct);
                return MapWithdrawal(consumedWithdrawal, goal.CurrentAmount);
            }

            Guid? destinationAccountId = null;
            if (dto.Reason == SavingsWithdrawalReason.ReallocatedToLiquid && dto.DestinationAccountId.HasValue)
            {
                var destination = await EnsureLiquidAccountAsync(userId, dto.DestinationAccountId.Value, ct);
                if (destination.Id == savingsAccount.Id)
                    throw new DomainException("SAME_SAVINGS_ACCOUNT", "Para mantener el dinero en ahorro, usa Liberar asignación.");
                if (savingsAccount.CurrentBalance < dto.Amount)
                    throw new DomainException("INSUFFICIENT_SAVINGS_ACCOUNT_BALANCE", "La cuenta de ahorro no tiene saldo real suficiente.");
                destinationAccountId = destination.Id;
            }

            var withdrawalCreated = await AddWithdrawalAsync(
                goal, dto.Amount, date, dto.Reason, dto.IdempotencyKey,
                dto.Notes, ct, destinationAccountId: destinationAccountId);
            if (destinationAccountId.HasValue)
            {
                await _accountService.SyncTransferBetweenAccountsAsync(
                    userId,
                    savingsAccount.Id,
                    FinancialAccountType.Savings,
                    destinationAccountId,
                    FinancialAccountType.Cash,
                    dto.Amount,
                    date,
                    "savings-goal-withdrawal",
                    withdrawalCreated.Id,
                    $"Retiro de meta: {goal.Name}",
                    ct);
            }
            return MapWithdrawal(withdrawalCreated, goal.CurrentAmount);
        }, cancellationToken);
    }

    private async Task<SavingsGoal> GetActiveGoalAsync(Guid id, Guid userId, CancellationToken ct)
        => await _savingsGoalRepository.GetByIdWithHistoryAsync(id, userId, ct) ?? throw new NotFoundException("Meta de ahorro", id);

    private async Task<SavingsGoal> GetArchiveTargetAsync(SavingsGoal source, Guid? targetId, Guid userId, CancellationToken ct)
    {
        if (!targetId.HasValue || targetId == Guid.Empty || targetId == source.Id) throw new DomainException("TARGET_GOAL_REQUIRED", "Selecciona otra meta activa como destino.");
        var target = await GetActiveGoalAsync(targetId.Value, userId, ct);
        await EnsureGoalSavingsAccountAsync(target, userId, ct);
        if (target.SavingsAccountId != source.SavingsAccountId)
            throw new DomainException("DIFFERENT_SAVINGS_ACCOUNTS", "Solo puedes reasignar entre metas respaldadas por la misma cuenta de ahorro.");
        return target;

    }

    private static void EnsureCapacity(SavingsGoal goal, decimal amount)
    {
        if (goal.CurrentAmount + amount > goal.TargetAmount) throw new DomainException("GOAL_TARGET_EXCEEDED", "La operación supera el objetivo de la meta; ajusta el monto o el objetivo primero.");
    }

    private async Task<SavingsGoalWithdrawal> AddWithdrawalAsync(SavingsGoal goal, decimal amount, DateOnly date, SavingsWithdrawalReason reason, Guid operationId, string? notes, CancellationToken ct, Guid? linkedExpenseId = null, Guid? destinationAccountId = null)
    {
        goal.CurrentAmount -= amount; goal.IsCompleted = false;
        var withdrawal = new SavingsGoalWithdrawal
        {
            Id = Guid.NewGuid(),
            SavingsGoalId = goal.Id,
            WithdrawalDate = date,
            Amount = amount,
            Reason = reason,
            Notes = notes?.Trim(),
            OperationId = operationId,
            LinkedExpenseId = reason == SavingsWithdrawalReason.Consumed ? linkedExpenseId : null,
            DestinationAccountId = destinationAccountId
        };
        await _savingsGoalRepository.AddWithdrawalAsync(withdrawal, ct);
        await _savingsGoalRepository.UpdateAsync(goal, ct);
        return withdrawal;
    }


    private Task<T> RunInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        => _unitOfWork == null ? action(cancellationToken) : _unitOfWork.ExecuteInTransactionAsync(action, cancellationToken);
    private async Task<Guid?> ValidateFundingAsync(
        Guid userId,
        FinancialAccountResponseDto savingsAccount,
        decimal amount,
        string mode,
        Guid? sourceAccountId,
        CancellationToken cancellationToken)
    {
        var allocated = (await _savingsGoalRepository.GetByUserIdAsync(userId, cancellationToken))
            .Where(goal => goal.SavingsAccountId == savingsAccount.Id)
            .Sum(goal => goal.CurrentAmount);

        if (allocated > savingsAccount.CurrentBalance)
            throw new DomainException(
                "SAVINGS_ACCOUNT_OVERALLOCATED",
                $"Las metas ya superan el saldo real de {savingsAccount.Name}. Concilia la cuenta antes de aportar.");

        if (string.Equals(mode, ExistingBalanceMode, StringComparison.OrdinalIgnoreCase))
        {
            if (allocated + amount > savingsAccount.CurrentBalance)
                throw new DomainException(
                    "INSUFFICIENT_UNALLOCATED_SAVINGS",
                    $"No hay saldo sin asignar suficiente en {savingsAccount.Name}.");
            return null;
        }

        if (!string.Equals(mode, AccountTransferMode, StringComparison.OrdinalIgnoreCase))
            throw new DomainException(
                "INVALID_FUNDING_MODE",
                "Elige asignar saldo existente o transferir dinero a la cuenta de ahorro.");
        if (!sourceAccountId.HasValue || sourceAccountId == Guid.Empty)
            throw new DomainException(
                "SOURCE_ACCOUNT_REQUIRED",
                "Selecciona la cuenta desde la que aportarás el dinero.");

        var source = await EnsureLiquidAccountAsync(userId, sourceAccountId.Value, cancellationToken);
        if (source.Id == savingsAccount.Id)
            throw new DomainException(
                "SAME_SAVINGS_ACCOUNT",
                "Para usar dinero que ya está en ahorro, selecciona Asignar saldo existente.");
        if (source.CurrentBalance < amount)
            throw new DomainException(
                "INSUFFICIENT_ACCOUNT_BALANCE",
                "La cuenta de origen no tiene saldo suficiente.");
        return source.Id;
    }

    private async Task<FinancialAccountResponseDto> EnsureGoalSavingsAccountAsync(
        SavingsGoal goal, Guid userId, CancellationToken cancellationToken)
    {
        if (!goal.SavingsAccountId.HasValue)
        {
            var defaultSavings = await _accountService.GetOrCreateDefaultAsync(
                userId, FinancialAccountType.Savings, cancellationToken);
            goal.SavingsAccountId = defaultSavings.Id;
            await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);
            return defaultSavings;
        }

        return await EnsureSavingsAccountAsync(
            userId, goal.SavingsAccountId.Value, cancellationToken);
    }

    private async Task<FinancialAccountResponseDto> EnsureSavingsAccountAsync(
        Guid userId, Guid accountId, CancellationToken cancellationToken)
    {
        var account = (await _accountService.GetAllAsync(userId, cancellationToken))
            .SingleOrDefault(item => item.Id == accountId)
            ?? throw new NotFoundException("Cuenta de ahorro", accountId);
        if (!account.IsActive)
            throw new DomainException("INACTIVE_SAVINGS_ACCOUNT", "La cuenta de ahorro seleccionada está inactiva.");
        if (!string.Equals(account.Type, "savings", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("INVALID_SAVINGS_ACCOUNT", "Las metas solo pueden respaldarse con una cuenta de tipo ahorro.");
        return account;
    }

    private async Task<FinancialAccountResponseDto> EnsureLiquidAccountAsync(
        Guid userId, Guid accountId, CancellationToken cancellationToken)
    {
        var account = (await _accountService.GetAllAsync(userId, cancellationToken))
            .SingleOrDefault(item => item.Id == accountId)
            ?? throw new NotFoundException("Cuenta", accountId);
        if (!account.IsActive)
            throw new DomainException("INACTIVE_ACCOUNT", "La cuenta seleccionada está inactiva.");
        if (!string.Equals(account.Type, "cash", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(account.Type, "savings", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("INVALID_LIQUID_ACCOUNT", "Selecciona una cuenta de efectivo o ahorro.");
        return account;
    }




    private static bool HasOpenRestorations(SavingsGoal goal) => goal.Restorations.Any(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted);

    private static SavingsGoalWithdrawalResponseDto MapWithdrawal(SavingsGoalWithdrawal withdrawal, decimal amountAfter) => new()
    {
        Id = withdrawal.Id,
        WithdrawalDate = withdrawal.WithdrawalDate,
        Amount = withdrawal.Amount,
        LinkedExpenseId = withdrawal.LinkedExpenseId,
        Reason = withdrawal.Reason,
        Notes = withdrawal.Notes,
        CreatedAt = withdrawal.CreatedAt,
        GoalCurrentAmountAfter = amountAfter
    };

    private static SavingsGoalResponseDto MapToResponseDto(SavingsGoal goal, FinancialAccountResponseDto? account) => new()
    {
        Id = goal.Id,
        SavingsAccountId = goal.SavingsAccountId,
        SavingsAccountName = account?.Name,
        SavingsAccountBalance = account?.CurrentBalance,
        Name = goal.Name,
        Description = goal.Description,
        TargetAmount = goal.TargetAmount,
        CurrentAmount = goal.CurrentAmount,
        RemainingAmount = goal.RemainingAmount,
        ProgressPercentage = goal.ProgressPercentage,
        TargetDate = goal.TargetDate,
        IsCompleted = goal.IsCompleted,
        Icon = goal.Icon,
        EstimatedMonthsToComplete = null,
        CreatedAt = goal.CreatedAt,
        Purpose = goal.Purpose == SavingsGoalPurpose.EmergencyFund ? "emergency_fund" : "general",
        MinimumProtectedAmount = goal.MinimumProtectedAmount,
        PendingRestorationAmount = goal.Restorations.Where(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted).Sum(r => r.OutstandingAmount),
        OpenRestorationsCount = goal.Restorations.Count(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted),
        NextRestorationDate = goal.Restorations.Where(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted).Select(r => (DateOnly?)r.NextScheduledDate).Min()
    };
}