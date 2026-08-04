using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Compromiso interno para restaurar un uso extraordinario del fondo de
/// emergencia. No es una deuda ni un pasivo financiero.
/// </summary>
public class EmergencyFundRestoration : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SavingsGoalId { get; set; }
    public Guid SourceWithdrawalId { get; set; }
    public Guid LinkedExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RestoredAmount { get; set; }
    public DateOnly TargetRestorationDate { get; set; }
    public decimal ScheduledContributionAmount { get; set; }
    public DateOnly NextScheduledDate { get; set; }
    public Guid? ScheduledSourceAccountId { get; set; }
    public EmergencyFundRestorationStatus Status { get; set; } = EmergencyFundRestorationStatus.Open;
    public DateOnly? CompletedDate { get; set; }
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
    public SavingsGoal SavingsGoal { get; set; } = null!;
    public SavingsGoalWithdrawal SourceWithdrawal { get; set; } = null!;
    public Expense LinkedExpense { get; set; } = null!;
    public ICollection<SavingsGoalContribution> Contributions { get; set; } = new List<SavingsGoalContribution>();

    public DateOnly? EstimatedCompletionDate
    {
        get
        {
            if (Status == EmergencyFundRestorationStatus.Completed)
                return CompletedDate;
            if (Status != EmergencyFundRestorationStatus.Open
                || OutstandingAmount <= 0
                || ScheduledContributionAmount <= 0)
                return null;

            var paymentsRemaining = (int)Math.Ceiling(OutstandingAmount / ScheduledContributionAmount);
            return AddMonths(NextScheduledDate, paymentsRemaining - 1);
        }
    }
    public decimal OutstandingAmount => Math.Max(OriginalAmount - RestoredAmount, 0);
    public bool IsOverdue => Status == EmergencyFundRestorationStatus.Open
        && NextScheduledDate < DateOnly.FromDateTime(DateTime.UtcNow);

    public void ApplyPayment(decimal amount, DateOnly paymentDate)
    {
        if (Status != EmergencyFundRestorationStatus.Open)
            throw new DomainException("RESTORATION_NOT_OPEN", "La restauraci\u00f3n ya no est\u00e1 abierta.");
        if (amount <= 0 || amount > OutstandingAmount)
            throw new DomainException("INVALID_RESTORATION_AMOUNT", "El aporte debe ser positivo y no superar el pendiente.");

        RestoredAmount += amount;
        if (OutstandingAmount == 0)
        {
            Status = EmergencyFundRestorationStatus.Completed;
            CompletedDate = paymentDate;
        }
        else if (paymentDate >= NextScheduledDate)
        {
            NextScheduledDate = AddMonths(NextScheduledDate, 1);
        }
    }

    private static DateOnly AddMonths(DateOnly date, int months)
    {
        var originalDay = date.Day;
        var targetMonth = date.AddDays(1 - date.Day).AddMonths(months);
        return new DateOnly(
            targetMonth.Year,
            targetMonth.Month,
            Math.Min(originalDay, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month)));
    }
}
