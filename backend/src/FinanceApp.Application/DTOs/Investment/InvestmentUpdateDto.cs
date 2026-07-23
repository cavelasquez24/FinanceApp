namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Ticker { get; set; }
    public string? Broker { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}