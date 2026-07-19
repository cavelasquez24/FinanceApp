namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentContributionResponseDto
{
    public Guid Id { get; set; }
    public DateOnly ContributionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
