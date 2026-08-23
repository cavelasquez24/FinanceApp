using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class SavingsReplenishmentService : ISavingsReplenishmentService
{
    private readonly ISavingsGoalRepository _savingsGoalRepository;
    private readonly ISavingsReplenishmentRepository _replenishmentRepository;
    private readonly IFinancialAccountService _accountService;
    private readonly IUserRepository _userRepository;
    private readonly IBusinessDateProvider _businessDateProvider;
    private readonly IUnitOfWork _unitOfWork;

    public SavingsReplenishmentService(
        ISavingsGoalRepository savingsGoalRepository,
        ISavingsReplenishmentRepository replenishmentRepository,
        IFinancialAccountService accountService,
        IUserRepository userRepository,
        IBusinessDateProvider businessDateProvider,
        IUnitOfWork unitOfWork)
    {
        _savingsGoalRepository = savingsGoalRepository;
        _replenishmentRepository = replenishmentRepository;
        _accountService = accountService;
        _userRepository = userRepository;
        _businessDateProvider = businessDateProvider;
        _unitOfWork = unitOfWork;
    }

    public Task<SavingsReplenishmentDto> CreateAsync(
        Guid userId, SavingsReplenishmentCreateDto dto, CancellationToken cancellationToken = default) =>
        _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new DomainException("INVALID_REPLENISHMENT_NAME", "El plan de reposición requiere un nombre.");
            if (dto.AmountTaken <= 0)
                throw new DomainException("INVALID_AMOUNT_TAKEN", "El monto tomado debe ser mayor a cero.");
            if (dto.MonthlyDebitAmount <= 0 || dto.MonthlyDebitAmount > dto.AmountTaken)
                throw new DomainException("INVALID_MONTHLY_DEBIT", "El débito por ciclo debe ser mayor a cero y no superar el monto tomado.");

            var goal = await _savingsGoalRepository.GetByIdWithHistoryAsync(dto.SavingsGoalId, userId, ct)
                ?? throw new NotFoundException("Meta de ahorro", dto.SavingsGoalId);
            var sourceAccount = await EnsureLiquidAccountAsync(userId, dto.SourceAccountId, ct);
            if (goal.SavingsAccountId.HasValue && sourceAccount.Id == goal.SavingsAccountId.Value)
                throw new DomainException(
                    "SAME_SAVINGS_ACCOUNT",
                    "La cuenta de origen del débito no puede ser la misma cuenta de ahorro de la meta.");

            var replenishment = new SavingsReplenishment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SavingsGoalId = goal.Id,
                SourceAccountId = sourceAccount.Id,
                Name = dto.Name.Trim(),
                Notes = dto.Notes?.Trim(),
                AmountTaken = dto.AmountTaken,
                AmountReplenished = 0m,
                MonthlyDebitAmount = dto.MonthlyDebitAmount,
                AutoDebitEnabled = dto.AutoDebitEnabled,
                IsPaused = false,
                Status = ReplenishmentStatus.Active
            };
            await _replenishmentRepository.CreateAsync(replenishment, ct);
            return Map(replenishment, goal.Name, sourceAccount.Name);
        }, cancellationToken);

    public async Task<ReplenishmentCycleResultDto> ExecuteCycleDebitsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var today = _businessDateProvider.Today;
        var (cycleMonth, cycleYear) = GetCurrentCycle(today, user?.PaydayDay);
        var (cycleStart, _) = GetCycleRange(cycleMonth, cycleYear, user?.PaydayDay);

        var plans = await _replenishmentRepository.GetActiveForAutoDebitAsync(userId, cancellationToken);
        var result = new ReplenishmentCycleResultDto();

        foreach (var plan in plans)
        {
            // Idempotencia por ciclo: si ya se debitó dentro del ciclo
            // actual, no se vuelve a ejecutar aunque se llame de nuevo.
            if (plan.LastDebitAt.HasValue && plan.LastDebitAt.Value >= cycleStart)
            {
                result.SkippedAlreadyDebitedCount++;
                continue;
            }

            var amount = Math.Min(plan.MonthlyDebitAmount, plan.PendingAmount);
            if (amount <= 0) continue;

            var accounts = await _accountService.GetAllAsync(userId, cancellationToken);
            var sourceAccount = accounts.SingleOrDefault(a => a.Id == plan.SourceAccountId);
            if (sourceAccount == null || sourceAccount.CurrentBalance < amount)
            {
                result.InsufficientFunds.Add(new ReplenishmentDebitFailureDto
                {
                    ReplenishmentId = plan.Id,
                    ReplenishmentName = plan.Name,
                    RequiredAmount = amount,
                    AvailableBalance = sourceAccount?.CurrentBalance ?? 0m
                });
                continue;
            }

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await ApplyDebitAsync(userId, plan, amount, today, DebitType.Automatic, null, ct);
                return true;
            }, cancellationToken);

            result.ProcessedCount++;
        }

        return result;
    }

    public Task<SavingsReplenishmentDto> ApplyManualDebitAsync(
        Guid userId, Guid replenishmentId, SavingsReplenishmentManualDebitDto dto,
        CancellationToken cancellationToken = default) =>
        _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var plan = await _replenishmentRepository.GetOwnedByIdAsync(replenishmentId, userId, ct)
                ?? throw new NotFoundException("Plan de reposición", replenishmentId);
            if (dto.IdempotencyKey == Guid.Empty)
                throw new DomainException("IDEMPOTENCY_REQUIRED", "El abono requiere una clave de idempotencia.");
            if (plan.Contributions.Any(c => c.OperationId == dto.IdempotencyKey))
                return Map(plan, plan.SavingsGoal.Name, plan.SourceAccount.Name);
            if (plan.Status != ReplenishmentStatus.Active && plan.Status != ReplenishmentStatus.Paused)
                throw new DomainException("REPLENISHMENT_NOT_ACTIVE", "El plan ya no acepta abonos.");
            if (dto.Amount <= 0 || dto.Amount > plan.PendingAmount)
                throw new DomainException("INVALID_DEBIT_AMOUNT", "El abono debe ser positivo y no superar el pendiente.");

            var accounts = await _accountService.GetAllAsync(userId, ct);
            var sourceAccount = accounts.SingleOrDefault(a => a.Id == plan.SourceAccountId)
                ?? throw new NotFoundException("Cuenta", plan.SourceAccountId);
            if (sourceAccount.CurrentBalance < dto.Amount)
                throw new DomainException("INSUFFICIENT_ACCOUNT_BALANCE", "La cuenta de origen no tiene saldo suficiente.");

            var today = _businessDateProvider.Today;
            await ApplyDebitAsync(userId, plan, dto.Amount, today, DebitType.Manual, dto.IdempotencyKey, ct, dto.Notes);
            return Map(plan, plan.SavingsGoal.Name, plan.SourceAccount.Name);
        }, cancellationToken);

    public async Task<SavingsReplenishmentDto> PauseAsync(
        Guid userId, Guid replenishmentId, SavingsReplenishmentPauseDto dto,
        CancellationToken cancellationToken = default)
    {
        var plan = await _replenishmentRepository.GetOwnedByIdAsync(replenishmentId, userId, cancellationToken)
            ?? throw new NotFoundException("Plan de reposición", replenishmentId);
        if (plan.Status != ReplenishmentStatus.Active)
            throw new DomainException("REPLENISHMENT_NOT_ACTIVE", "Solo un plan activo puede pausarse.");

        plan.IsPaused = true;
        plan.Status = ReplenishmentStatus.Paused;
        if (!string.IsNullOrWhiteSpace(dto.Reason))
        {
            plan.Notes = string.IsNullOrWhiteSpace(plan.Notes)
                ? dto.Reason.Trim()
                : $"{plan.Notes} · Pausado: {dto.Reason.Trim()}";
        }
        await _replenishmentRepository.UpdateAsync(plan, cancellationToken);
        return Map(plan, plan.SavingsGoal.Name, plan.SourceAccount.Name);
    }

    public async Task<SavingsReplenishmentDto> ResumeAsync(
        Guid userId, Guid replenishmentId, CancellationToken cancellationToken = default)
    {
        var plan = await _replenishmentRepository.GetOwnedByIdAsync(replenishmentId, userId, cancellationToken)
            ?? throw new NotFoundException("Plan de reposición", replenishmentId);
        if (plan.Status != ReplenishmentStatus.Paused)
            throw new DomainException("REPLENISHMENT_NOT_PAUSED", "Solo un plan pausado puede reanudarse.");

        plan.IsPaused = false;
        plan.Status = ReplenishmentStatus.Active;
        await _replenishmentRepository.UpdateAsync(plan, cancellationToken);
        return Map(plan, plan.SavingsGoal.Name, plan.SourceAccount.Name);
    }

    public async Task CancelAsync(
        Guid userId, Guid replenishmentId, CancellationToken cancellationToken = default)
    {
        var plan = await _replenishmentRepository.GetOwnedByIdAsync(replenishmentId, userId, cancellationToken)
            ?? throw new NotFoundException("Plan de reposición", replenishmentId);
        if (plan.Status is ReplenishmentStatus.Completed or ReplenishmentStatus.Cancelled)
            throw new DomainException("REPLENISHMENT_ALREADY_CLOSED", "El plan ya está cerrado.");

        plan.Status = ReplenishmentStatus.Cancelled;
        plan.IsPaused = false;
        await _replenishmentRepository.UpdateAsync(plan, cancellationToken);
    }

    public async Task<IReadOnlyList<SavingsReplenishmentDto>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var plans = await _replenishmentRepository.GetByUserIdAsync(userId, cancellationToken);
        return plans.Select(p => Map(p, p.SavingsGoal.Name, p.SourceAccount.Name)).ToList();
    }

    public async Task<IReadOnlyList<SavingsReplenishmentDto>> GetByGoalIdAsync(
        Guid userId, Guid goalId, CancellationToken cancellationToken = default)
    {
        var plans = await _replenishmentRepository.GetByGoalIdAsync(goalId, userId, cancellationToken);
        return plans.Select(p => Map(p, p.SavingsGoal.Name, p.SourceAccount.Name)).ToList();
    }

    public async Task<SavingsReplenishmentDto> GetByIdAsync(
        Guid userId, Guid replenishmentId, CancellationToken cancellationToken = default)
    {
        var plan = await _replenishmentRepository.GetOwnedByIdAsync(replenishmentId, userId, cancellationToken)
            ?? throw new NotFoundException("Plan de reposición", replenishmentId);
        return Map(plan, plan.SavingsGoal.Name, plan.SourceAccount.Name);
    }

    /// <summary>
    /// Núcleo atómico compartido por el débito automático y el manual:
    /// mueve el saldo real (SourceAccount → cuenta de ahorro de la meta),
    /// incrementa SavingsGoal.CurrentAmount y registra el aporte como
    /// SavingsGoalContribution vinculado a este plan. Neutro en
    /// patrimonio — solo redistribuye entre las dos cuentas.
    /// </summary>
    private async Task ApplyDebitAsync(
        Guid userId, SavingsReplenishment plan, decimal amount, DateOnly date,
        DebitType type, Guid? idempotencyKey, CancellationToken ct, string? notesOverride = null)
    {
        var goal = plan.SavingsGoal;
        if (!goal.SavingsAccountId.HasValue)
            throw new DomainException("SAVINGS_ACCOUNT_REQUIRED", "La meta no tiene cuenta de ahorro asociada.");

        goal.CurrentAmount += amount;
        goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount;

        var contribution = new SavingsGoalContribution
        {
            Id = Guid.NewGuid(),
            SavingsGoalId = goal.Id,
            ContributionDate = date,
            Amount = amount,
            Notes = notesOverride?.Trim() ?? $"Reposición: {plan.Name}",
            SavingsReplenishmentId = plan.Id,
            DebitType = type,
            SourceAccountId = plan.SourceAccountId,
            OperationId = idempotencyKey
        };
        await _savingsGoalRepository.AddContributionAsync(contribution, ct);

        plan.AmountReplenished += amount;
        plan.LastDebitAt = date;
        if (plan.PendingAmount <= 0)
        {
            plan.Status = ReplenishmentStatus.Completed;
            plan.CompletedAt = date;
        }
        await _replenishmentRepository.UpdateAsync(plan, ct);
        await _savingsGoalRepository.UpdateAsync(goal, ct);

        await _accountService.SyncTransferBetweenAccountsAsync(
            userId,
            plan.SourceAccountId, FinancialAccountType.Cash,
            goal.SavingsAccountId, FinancialAccountType.Savings,
            amount, date,
            "savings-replenishment-debit", contribution.Id,
            $"Reposición: {plan.Name}", ct);
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

    private static SavingsReplenishmentDto Map(SavingsReplenishment r, string goalName, string accountName) => new()
    {
        Id = r.Id,
        SavingsGoalId = r.SavingsGoalId,
        SavingsGoalName = goalName,
        SourceAccountId = r.SourceAccountId,
        SourceAccountName = accountName,
        Name = r.Name,
        Notes = r.Notes,
        AmountTaken = r.AmountTaken,
        AmountReplenished = r.AmountReplenished,
        PendingAmount = r.PendingAmount,
        MonthlyDebitAmount = r.MonthlyDebitAmount,
        ProgressPercent = r.AmountTaken > 0 ? Math.Round(r.AmountReplenished / r.AmountTaken * 100, 2) : 0,
        EstimatedCyclesRemaining = r.MonthlyDebitAmount > 0
            ? (int)Math.Ceiling(r.PendingAmount / r.MonthlyDebitAmount)
            : 0,
        AutoDebitEnabled = r.AutoDebitEnabled,
        IsPaused = r.IsPaused,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
        CompletedAt = r.CompletedAt,
        LastDebitAt = r.LastDebitAt,
        Debits = r.Contributions
            .Where(c => c.DeletedAt == null)
            .OrderByDescending(c => c.ContributionDate)
            .Select(c => new ReplenishmentDebitDto
            {
                Id = c.Id,
                Amount = c.Amount,
                DebitDate = c.ContributionDate,
                Type = c.DebitType ?? DebitType.Manual,
                Notes = c.Notes
            }).ToList()
    };

    // TODO: consolidar GetCycleRange — este mismo cálculo está duplicado
    // en CurrentDashboardService, DashboardService y BudgetService.
    private static (int Month, int Year) GetCurrentCycle(DateOnly today, int? paydayDay)
    {
        if (paydayDay is null) return (today.Month, today.Year);
        var day = Math.Min(paydayDay.Value, DateTime.DaysInMonth(today.Year, today.Month));
        if (today.Day >= day) return (today.Month, today.Year);
        var previous = today.AddMonths(-1);
        return (previous.Month, previous.Year);
    }

    private static (DateOnly Start, DateOnly End) GetCycleRange(int month, int year, int? paydayDay)
    {
        if (paydayDay is null)
            return (
                new DateOnly(year, month, 1),
                new DateOnly(year, month, DateTime.DaysInMonth(year, month)));

        var day = Math.Min(paydayDay.Value, DateTime.DaysInMonth(year, month));
        var start = new DateOnly(year, month, day);
        var next = start.AddMonths(1);
        var nextDay = Math.Min(paydayDay.Value, DateTime.DaysInMonth(next.Year, next.Month));
        return (start, new DateOnly(next.Year, next.Month, nextDay).AddDays(-1));
    }
}
