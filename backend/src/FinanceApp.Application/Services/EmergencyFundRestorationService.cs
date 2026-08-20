using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.Expense;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public partial class EmergencyFundRestorationService : IEmergencyFundRestorationService
{
    private readonly ISavingsGoalRepository _savingsGoalRepository;
    private readonly IEmergencyFundRestorationRepository _restorationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExpenseService _expenseService;
    private readonly IFinancialAccountService _accountService;

    public EmergencyFundRestorationService(
        ISavingsGoalRepository savingsGoalRepository,
        IEmergencyFundRestorationRepository restorationRepository,
        IExpenseService expenseService,
        IFinancialAccountService accountService,
        IUnitOfWork unitOfWork)
    {
        _savingsGoalRepository = savingsGoalRepository;
        _restorationRepository = restorationRepository;
        _unitOfWork = unitOfWork;
        _expenseService = expenseService;
        _accountService = accountService;
    }

    public async Task<IReadOnlyList<EmergencyFundRestorationResponseDto>> GetByGoalAsync(
        Guid goalId, Guid userId, CancellationToken cancellationToken = default)
    {
        await GetEmergencyFundAsync(goalId, userId, cancellationToken);
        var restorations = await _restorationRepository.GetByGoalAsync(goalId, userId, cancellationToken);
        return restorations.Select(Map).ToList();
    }

    public Task<EmergencyFundRestorationResponseDto> CreateUseAsync(
        Guid goalId, Guid userId, EmergencyFundUseCreateDto dto,
        CancellationToken cancellationToken = default) =>
        _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var goal = await GetEmergencyFundAsync(goalId, userId, ct);
            ValidateUse(goal, dto);
            if (dto.IdempotencyKey == Guid.Empty)
                throw new DomainException("IDEMPOTENCY_REQUIRED", "El uso del fondo requiere una clave de idempotencia.");
            var existingWithdrawal = goal.Withdrawals.SingleOrDefault(item => item.OperationId == dto.IdempotencyKey);
            if (existingWithdrawal != null)
            {
                var existingRestoration = goal.Restorations
                    .SingleOrDefault(item => item.SourceWithdrawalId == existingWithdrawal.Id);
                if (existingRestoration != null) return Map(existingRestoration);
                throw new DomainException("INCOMPLETE_EMERGENCY_USE", "El uso anterior no tiene una reposición asociada.");
            }
            var savingsAccount = await EnsureGoalSavingsAccountAsync(goal, userId, ct);
            Guid? destinationAccountId = null;
            if (string.Equals(dto.UseMode, "expense", StringComparison.OrdinalIgnoreCase))
            {
                if (!dto.ExpenseCategoryId.HasValue)
                    throw new DomainException("EXPENSE_CATEGORY_REQUIRED", "Selecciona la categoría del gasto de emergencia.");
            }
            else if (string.Equals(dto.UseMode, "account_transfer", StringComparison.OrdinalIgnoreCase))
            {
                if (!dto.DestinationAccountId.HasValue)
                    throw new DomainException("DESTINATION_ACCOUNT_REQUIRED", "Selecciona la cuenta que recibirá el dinero.");
                var destination = await EnsureLiquidAccountAsync(userId, dto.DestinationAccountId.Value, ct);
                if (destination.Id == savingsAccount.Id)
                    throw new DomainException("SAME_SAVINGS_ACCOUNT", "Selecciona una cuenta distinta a la cuenta de ahorro.");
                destinationAccountId = destination.Id;
            }
            else
                throw new DomainException("INVALID_EMERGENCY_USE_MODE", "Elige registrar el gasto o transferir el dinero.");

            if (savingsAccount.CurrentBalance < dto.FundedAmount)
                throw new DomainException("INSUFFICIENT_SAVINGS_ACCOUNT_BALANCE", "La cuenta de ahorro no tiene saldo real suficiente.");

            goal.CurrentAmount -= dto.FundedAmount;
            goal.IsCompleted = false;

            var withdrawal = new SavingsGoalWithdrawal
            {
                SavingsGoalId = goal.Id,
                Id = Guid.NewGuid(),
                WithdrawalDate = dto.AcquisitionDate,
                Amount = dto.FundedAmount,
                Reason = destinationAccountId.HasValue ? SavingsWithdrawalReason.ReallocatedToLiquid : SavingsWithdrawalReason.Consumed,
                Notes = dto.Notes?.Trim(),
                OperationId = dto.IdempotencyKey,
                DestinationAccountId = destinationAccountId
            };
            await _savingsGoalRepository.AddWithdrawalAsync(withdrawal, ct);
            if (string.Equals(dto.UseMode, "expense", StringComparison.OrdinalIgnoreCase))
            {
                var expense = await _expenseService.CreateAsync(userId, new ExpenseCreateDto
                {
                    CategoryId = dto.ExpenseCategoryId!.Value,
                    AccountId = savingsAccount.Id,
                    IdempotencyKey = dto.IdempotencyKey,
                    Amount = dto.FundedAmount,
                    Description = dto.Description.Trim(),
                    Date = dto.AcquisitionDate,
                    PaymentMethod = "cash",
                    IsRecurring = false,
                    Notes = dto.Notes?.Trim()
                }, ct);
                withdrawal.LinkedExpenseId = expense.Id;
            }
            else
            {
                await _accountService.SyncTransferBetweenAccountsAsync(
                    userId,
                    savingsAccount.Id,
                    FinancialAccountType.Savings,
                    destinationAccountId,
                    FinancialAccountType.Cash,
                    dto.FundedAmount,
                    dto.AcquisitionDate,
                    "emergency-fund-use",
                    withdrawal.Id,
                    $"Uso del fondo: {dto.Description.Trim()}",
                    ct);
            }

            var restoration = new EmergencyFundRestoration
            {
                UserId = userId,
                Id = Guid.NewGuid(),
                SavingsGoalId = goal.Id,
                SourceWithdrawalId = withdrawal.Id,
                LinkedExpenseId = withdrawal.LinkedExpenseId,
                SavingsGoal = goal,
                SourceWithdrawal = withdrawal,
                Description = dto.Description.Trim(),
                AcquisitionDate = dto.AcquisitionDate,
                OriginalAmount = dto.FundedAmount,
                TargetRestorationDate = dto.TargetRestorationDate,
                ScheduledContributionAmount = dto.ScheduledContributionAmount,
                NextScheduledDate = dto.FirstScheduledDate,
                Notes = dto.Notes?.Trim()
            };
            await _restorationRepository.CreateAsync(restoration, ct);
            await _savingsGoalRepository.UpdateAsync(goal, ct);
            return Map(restoration);
        }, cancellationToken);

    public Task<EmergencyFundRestorationResponseDto> RegisterPaymentAsync(
        Guid restorationId, Guid userId, EmergencyFundRestorationPaymentDto dto,
        CancellationToken cancellationToken = default) =>
        _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var restoration = await _restorationRepository.GetOwnedByIdAsync(restorationId, userId, ct)
                ?? throw new NotFoundException("Restauración del fondo", restorationId);
            if (dto.IdempotencyKey == Guid.Empty)
                throw new DomainException("IDEMPOTENCY_REQUIRED", "La reposición requiere una clave de idempotencia.");
            if (restoration.Contributions.Any(item => item.OperationId == dto.IdempotencyKey))
                return Map(restoration);

            if (restoration.Status != EmergencyFundRestorationStatus.Open)
                throw new DomainException("RESTORATION_NOT_OPEN", "La restauración ya no está abierta.");
            if (dto.Amount <= 0 || dto.Amount > restoration.OutstandingAmount)
                throw new DomainException("INVALID_RESTORATION_AMOUNT", "El aporte debe ser positivo y no superar el pendiente.");
            if (restoration.SavingsGoal.CurrentAmount + dto.Amount > restoration.SavingsGoal.TargetAmount)
                throw new DomainException("RESTORATION_EXCEEDS_GOAL", "El aporte supera el espacio disponible en el fondo.");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (dto.PaymentDate < restoration.AcquisitionDate || dto.PaymentDate > today)
                throw new DomainException(
                    "INVALID_RESTORATION_PAYMENT_DATE",
                    "La fecha del aporte debe estar entre la adquisici\u00f3n y hoy.");
            var savingsAccount = await EnsureGoalSavingsAccountAsync(
                restoration.SavingsGoal, userId, ct);
            var sourceAccountId = await ValidateRestorationFundingAsync(
                userId, savingsAccount, dto.Amount, dto.FundingMode, dto.SourceAccountId, ct);


            var contribution = new SavingsGoalContribution
            {
                SavingsGoalId = restoration.SavingsGoalId,
                EmergencyFundRestorationId = restoration.Id,
                Id = Guid.NewGuid(),
                ContributionDate = dto.PaymentDate,
                Amount = dto.Amount,
                Notes = dto.Notes?.Trim() ?? $"Restauración: {restoration.Description}",
                OperationId = dto.IdempotencyKey,
                SourceAccountId = sourceAccountId
            };

            restoration.ApplyPayment(dto.Amount, dto.PaymentDate);
            restoration.SavingsGoal.CurrentAmount += dto.Amount;
            restoration.SavingsGoal.IsCompleted =
                restoration.SavingsGoal.CurrentAmount >= restoration.SavingsGoal.TargetAmount;


            await _savingsGoalRepository.AddContributionAsync(contribution, ct);
            restoration.ScheduledSourceAccountId = sourceAccountId;
            if (sourceAccountId.HasValue)
            {
                await _accountService.SyncTransferBetweenAccountsAsync(
                    userId,
                    sourceAccountId,
                    FinancialAccountType.Cash,
                    savingsAccount.Id,
                    FinancialAccountType.Savings,
                    dto.Amount,
                    dto.PaymentDate,
                    "emergency-fund-restoration",
                    contribution.Id,
                    $"Reposición: {restoration.Description}",
                    ct);
            }
            await _restorationRepository.UpdateAsync(restoration, ct);
            return Map(restoration);
        }, cancellationToken);

    public async Task<EmergencyFundRestorationResponseDto> CancelAsync(
        Guid restorationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var restoration = await _restorationRepository.GetOwnedByIdAsync(restorationId, userId, cancellationToken)
            ?? throw new NotFoundException("Restauración del fondo", restorationId);
        if (restoration.Status != EmergencyFundRestorationStatus.Open)
            throw new DomainException("RESTORATION_NOT_OPEN", "La restauración ya no está abierta.");
        restoration.Status = EmergencyFundRestorationStatus.Cancelled;
        await _restorationRepository.UpdateAsync(restoration, cancellationToken);
        return Map(restoration);
    }

    private async Task<SavingsGoal> GetEmergencyFundAsync(
        Guid goalId, Guid userId, CancellationToken cancellationToken)
    {
        var goal = await _savingsGoalRepository.GetByIdWithHistoryAsync(goalId, userId, cancellationToken);
        if (goal == null || goal.IsDeleted)
            throw new NotFoundException("Fondo de emergencia", goalId);
        if (goal.Purpose != SavingsGoalPurpose.EmergencyFund)
            throw new DomainException("NOT_EMERGENCY_FUND", "Esta operación solo está disponible para el fondo de emergencia.");
        return goal;
    }

    private async Task<Guid?> ValidateRestorationFundingAsync(
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

        if (string.Equals(mode, "existing_balance", StringComparison.OrdinalIgnoreCase))
        {
            if (allocated + amount > savingsAccount.CurrentBalance)
                throw new DomainException(
                    "INSUFFICIENT_UNALLOCATED_SAVINGS",
                    $"No hay saldo sin asignar suficiente en {savingsAccount.Name}.");
            return null;
        }

        if (!string.Equals(mode, "account_transfer", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("INVALID_FUNDING_MODE", "Elige saldo existente o transferencia.");
        if (!sourceAccountId.HasValue)
            throw new DomainException("SOURCE_ACCOUNT_REQUIRED", "Selecciona la cuenta de origen.");

        var source = await EnsureLiquidAccountAsync(userId, sourceAccountId.Value, cancellationToken);
        if (source.Id == savingsAccount.Id)
            throw new DomainException("SAME_SAVINGS_ACCOUNT", "Para usar el saldo de ahorro selecciona saldo existente.");
        if (source.CurrentBalance < amount)
            throw new DomainException("INSUFFICIENT_ACCOUNT_BALANCE", "La cuenta de origen no tiene saldo suficiente.");
        return source.Id;
    }

    private async Task<FinancialAccountResponseDto> EnsureGoalSavingsAccountAsync(
        SavingsGoal goal, Guid userId, CancellationToken cancellationToken)
    {
        if (!goal.SavingsAccountId.HasValue)
        {
            var account = await _accountService.GetOrCreateDefaultAsync(
                userId, FinancialAccountType.Savings, cancellationToken);
            goal.SavingsAccountId = account.Id;
            await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);
            return account;
        }

        var savings = (await _accountService.GetAllAsync(userId, cancellationToken))
            .SingleOrDefault(account => account.Id == goal.SavingsAccountId)
            ?? throw new NotFoundException("Cuenta de ahorro", goal.SavingsAccountId.Value);
        if (!savings.IsActive || !string.Equals(savings.Type, "savings", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("INVALID_SAVINGS_ACCOUNT", "La cuenta de ahorro vinculada no está disponible.");
        return savings;
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

    private static void ValidateUse(SavingsGoal goal, EmergencyFundUseCreateDto dto)
    {
        if (dto.FundedAmount <= 0 || dto.FundedAmount > goal.CurrentAmount)
            throw new DomainException("INVALID_FUNDED_AMOUNT", "El monto financiado debe ser positivo y no superar el fondo disponible.");
        if (string.IsNullOrWhiteSpace(dto.Description))
            throw new DomainException("INVALID_DESCRIPTION", "Describe el bien, servicio o emergencia financiada.");
        if (dto.TargetRestorationDate < dto.AcquisitionDate)
            throw new DomainException("INVALID_TARGET_DATE", "La fecha objetivo no puede ser anterior al uso del fondo.");
        if (dto.FirstScheduledDate < dto.AcquisitionDate || dto.FirstScheduledDate > dto.TargetRestorationDate)
            throw new DomainException("INVALID_FIRST_SCHEDULED_DATE", "La primera fecha programada debe estar dentro del plazo de restauración.");
        if (dto.ScheduledContributionAmount <= 0)
            throw new DomainException("INVALID_SCHEDULED_AMOUNT", "El aporte programado debe ser mayor a cero.");
        if (dto.ScheduledContributionAmount * CountMonthlyOccurrences(dto.FirstScheduledDate, dto.TargetRestorationDate) < dto.FundedAmount)
            throw new DomainException("INSUFFICIENT_RESTORATION_PLAN", "El aporte programado no alcanza a restaurar el fondo antes de la fecha objetivo.");

    }

    private static int CountMonthlyOccurrences(DateOnly firstDate, DateOnly targetDate)
    {
        var count = 0;
        var cursor = firstDate;
        while (cursor <= targetDate)
        {
            count++;
            cursor = cursor.AddMonths(1);
        }
        return count;
    }


    private static EmergencyFundRestorationResponseDto Map(EmergencyFundRestoration restoration) => new()
    {
        Id = restoration.Id,
        SavingsGoalId = restoration.SavingsGoalId,
        LinkedExpenseId = restoration.LinkedExpenseId,
        Description = restoration.Description,
        AcquisitionDate = restoration.AcquisitionDate,
        OriginalAmount = restoration.OriginalAmount,
        RestoredAmount = restoration.RestoredAmount,
        OutstandingAmount = restoration.OutstandingAmount,
        TargetRestorationDate = restoration.TargetRestorationDate,
        ScheduledContributionAmount = restoration.ScheduledContributionAmount,
        NextContributionAmount = Math.Min(restoration.ScheduledContributionAmount, restoration.OutstandingAmount),
        NextScheduledDate = restoration.NextScheduledDate,
        ScheduledSourceAccountId = restoration.ScheduledSourceAccountId,
        Status = restoration.Status.ToString().ToLowerInvariant(),
        EstimatedCompletionDate = restoration.EstimatedCompletionDate,
        CompletedDate = restoration.CompletedDate,
        IsOverdue = restoration.IsOverdue,
        Notes = restoration.Notes
    };
}
