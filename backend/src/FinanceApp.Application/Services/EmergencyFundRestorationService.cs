using FinanceApp.Application.DTOs.Expense;
using FinanceApp.Application.DTOs.SavingsGoal;
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
    private readonly IExpenseService _expenseService;
    private readonly IFinancialAccountService _accountService;
    private readonly IUnitOfWork _unitOfWork;

    public EmergencyFundRestorationService(
        ISavingsGoalRepository savingsGoalRepository,
        IEmergencyFundRestorationRepository restorationRepository,
        IExpenseService expenseService,
        IFinancialAccountService accountService,
        IUnitOfWork unitOfWork)
    {
        _savingsGoalRepository = savingsGoalRepository;
        _restorationRepository = restorationRepository;
        _expenseService = expenseService;
        _accountService = accountService;
        _unitOfWork = unitOfWork;
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
            var paymentAccountBalance = await _accountService.GetAvailableBalanceAsync(
                userId, dto.ExpenseAccountId, FinancialAccountType.Cash, ct);
            var budgetFundedAmount = dto.ExpenseAmount - dto.FundedAmount;
            if (paymentAccountBalance < budgetFundedAmount)
                throw new DomainException(
                    "INSUFFICIENT_PAYMENT_ACCOUNT_BALANCE",
                    "La cuenta de pago no tiene saldo suficiente para cubrir la parte no financiada por el fondo.");

            var scheduledSourceAccountId =
                dto.ScheduledSourceAccountId ?? dto.ExpenseAccountId;
            await _accountService.GetAvailableBalanceAsync(
                userId, scheduledSourceAccountId, FinancialAccountType.Cash, ct);


            goal.CurrentAmount -= dto.FundedAmount;
            goal.IsCompleted = false;

            var withdrawal = new SavingsGoalWithdrawal
            {
                SavingsGoalId = goal.Id,
                WithdrawalDate = dto.AcquisitionDate,
                Amount = dto.FundedAmount,
                Reason = SavingsWithdrawalReason.Consumed,
                Notes = dto.Notes?.Trim()
            };
            await _savingsGoalRepository.AddWithdrawalAsync(withdrawal, ct);

            await _accountService.SyncTransferBetweenAccountsAsync(
                userId, null, FinancialAccountType.Savings,
                dto.ExpenseAccountId, FinancialAccountType.Cash,
                dto.FundedAmount, dto.AcquisitionDate,
                "emergency-fund-use", withdrawal.Id,
                $"Uso del fondo: {dto.Description.Trim()}", ct);

            var expense = await _expenseService.CreateAsync(userId, new ExpenseCreateDto
            {
                CategoryId = dto.CategoryId,
                AccountId = dto.ExpenseAccountId,
                Amount = dto.ExpenseAmount,
                Description = dto.Description.Trim(),
                Date = dto.AcquisitionDate,
                PaymentMethod = dto.PaymentMethod,
                IsRecurring = false,
                Notes = dto.Notes?.Trim()
            }, ct);

            withdrawal.LinkedExpenseId = expense.Id;
            var restoration = new EmergencyFundRestoration
            {
                UserId = userId,
                SavingsGoalId = goal.Id,
                SourceWithdrawalId = withdrawal.Id,
                LinkedExpenseId = expense.Id,
                Description = dto.Description.Trim(),
                AcquisitionDate = dto.AcquisitionDate,
                OriginalAmount = dto.FundedAmount,
                TargetRestorationDate = dto.TargetRestorationDate,
                ScheduledContributionAmount = dto.ScheduledContributionAmount,
                NextScheduledDate = dto.FirstScheduledDate,
                ScheduledSourceAccountId = scheduledSourceAccountId,
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

            var sourceAccountId = dto.SourceAccountId ?? restoration.ScheduledSourceAccountId;
            var availableBalance = await _accountService.GetAvailableBalanceAsync(
                userId, sourceAccountId, FinancialAccountType.Cash, ct);
            if (availableBalance < dto.Amount)
                throw new DomainException(
                    "INSUFFICIENT_RESTORATION_FUNDS",
                    "La cuenta de origen no tiene saldo suficiente para realizar el aporte.");

            var contribution = new SavingsGoalContribution
            {
                SavingsGoalId = restoration.SavingsGoalId,
                EmergencyFundRestorationId = restoration.Id,
                ContributionDate = dto.PaymentDate,
                Amount = dto.Amount,
                Notes = dto.Notes?.Trim() ?? $"Restauración: {restoration.Description}"
            };

            restoration.ApplyPayment(dto.Amount, dto.PaymentDate);
            restoration.SavingsGoal.CurrentAmount += dto.Amount;
            restoration.SavingsGoal.IsCompleted =
                restoration.SavingsGoal.CurrentAmount >= restoration.SavingsGoal.TargetAmount;


            await _savingsGoalRepository.AddContributionAsync(contribution, ct);
            await _accountService.SyncTransferBetweenAccountsAsync(
                userId, sourceAccountId, FinancialAccountType.Cash,
                null, FinancialAccountType.Savings,
                dto.Amount, dto.PaymentDate,
                "emergency-fund-restoration", contribution.Id,
                $"Restauración: {restoration.Description}", ct);
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

    private static void ValidateUse(SavingsGoal goal, EmergencyFundUseCreateDto dto)
    {
        if (dto.FundedAmount <= 0 || dto.FundedAmount > goal.CurrentAmount)
            throw new DomainException("INVALID_FUNDED_AMOUNT", "El monto financiado debe ser positivo y no superar el fondo disponible.");
        if (dto.ExpenseAmount < dto.FundedAmount)
            throw new DomainException("INVALID_EXPENSE_AMOUNT", "El gasto no puede ser menor al monto tomado del fondo.");
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
