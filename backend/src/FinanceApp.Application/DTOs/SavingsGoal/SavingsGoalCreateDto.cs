namespace FinanceApp.Application.DTOs.SavingsGoal;

public class SavingsGoalCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal InitialAmount { get; set; } = 0;
    public Guid? InitialSourceAccountId { get; set; }
    public DateOnly? InitialFundingDate { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public DateOnly? TargetDate { get; set; }
    public string? Icon { get; set; }
    public string Purpose { get; set; } = "general";
    public decimal? MinimumProtectedAmount { get; set; }
}