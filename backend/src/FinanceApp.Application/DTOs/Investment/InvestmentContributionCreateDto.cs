namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentContributionCreateDto
{
    public DateOnly? ContributionDate { get; set; }  // null → hoy
    public decimal Amount { get; set; }               // > 0
    public string? Notes { get; set; }
}
