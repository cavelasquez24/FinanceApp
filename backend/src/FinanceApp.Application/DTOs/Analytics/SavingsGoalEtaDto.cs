namespace FinanceApp.Application.DTOs.Analytics;

public class SavingsGoalEtaDto
{
    public Guid GoalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal Remaining { get; set; }
    public decimal ProgressPct { get; set; }
    public decimal AvgMonthlyContribution { get; set; }
    public int? EstimatedMonthsToGoal { get; set; }
    public DateOnly? EstimatedCompletionDate { get; set; }
    public bool IsOnTrack { get; set; }
}
