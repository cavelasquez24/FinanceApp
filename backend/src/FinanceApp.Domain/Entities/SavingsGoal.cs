namespace FinanceApp.Domain.Entities;

public class SavingsGoal : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; } = 0;
    public DateOnly? TargetDate { get; set; }
    public bool IsCompleted { get; set; } = false;
    public string? Icon { get; set; }

    // Propiedad de navegación
    public User User { get; set; } = null!;

    // v2.0.1 — historial de movimientos (SavingsGoalContribution/Withdrawal)
    public ICollection<SavingsGoalContribution> Contributions { get; set; } = new List<SavingsGoalContribution>();
    public ICollection<SavingsGoalWithdrawal> Withdrawals { get; set; } = new List<SavingsGoalWithdrawal>();

    // Calculadas en memoria
    public decimal ProgressPercentage => TargetAmount > 0
        ? Math.Min((CurrentAmount / TargetAmount) * 100, 100)
        : 0;

    public decimal RemainingAmount => Math.Max(TargetAmount - CurrentAmount, 0);
}
