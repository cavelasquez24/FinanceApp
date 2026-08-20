namespace FinanceApp.Application.DTOs.SavingsGoal;

public class DepositDto
{
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string FundingMode { get; set; } = "account_transfer";
    public Guid? SourceAccountId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public DateOnly? ContributionDate { get; set; }
}