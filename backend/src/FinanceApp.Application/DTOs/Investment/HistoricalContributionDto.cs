namespace FinanceApp.Application.DTOs.Investment;

public class HistoricalContributionDto
{
    public DateOnly ContributionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
