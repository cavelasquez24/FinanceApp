namespace FinanceApp.Application.DTOs.SavingsGoal;

public class SavingsGoalUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public DateOnly? TargetDate { get; set; }
    public string? Icon { get; set; }
}