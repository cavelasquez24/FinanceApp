namespace FinanceApp.Application.DTOs.SavingsGoal;

public class SavingsGoalResponseDto
{
    public Guid Id { get; set; }
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
    public DateTimeOffset CreatedAt { get; set; }
}