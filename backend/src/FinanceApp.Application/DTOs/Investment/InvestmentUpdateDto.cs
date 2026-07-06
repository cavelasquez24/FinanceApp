namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public string? Broker { get; set; }
    public decimal CurrentValue { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}