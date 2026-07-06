namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public string? Broker { get; set; }
    public decimal InitialAmount { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal GainLoss { get; set; }
    public decimal GainLossPercentage { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}