namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public string? Broker { get; set; }
    public decimal InitialAmount { get; set; }
    public decimal? CurrentValue { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public bool IsHistoricalImport { get; set; }
    public string? Notes { get; set; }
}