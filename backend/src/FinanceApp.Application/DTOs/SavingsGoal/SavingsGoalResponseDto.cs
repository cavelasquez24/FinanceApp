namespace FinanceApp.Application.DTOs.SavingsGoal;

public class SavingsGoalResponseDto
{
    public Guid Id { get; set; }
    public Guid? SavingsAccountId { get; set; }
    public string? SavingsAccountName { get; set; }
    public decimal? SavingsAccountBalance { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal ProgressPercentage { get; set; }
    public DateOnly? TargetDate { get; set; }
    public bool IsCompleted { get; set; }
    public string? Icon { get; set; }
    public int? EstimatedMonthsToComplete { get; set; }
    public string Purpose { get; set; } = "general";
    public decimal? MinimumProtectedAmount { get; set; }
    public decimal PendingRestorationAmount { get; set; }
    public int OpenRestorationsCount { get; set; }
    public DateOnly? NextRestorationDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}