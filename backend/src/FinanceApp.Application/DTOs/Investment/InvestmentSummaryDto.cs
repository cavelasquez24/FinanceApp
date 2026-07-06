namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentSummaryDto
{
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalGain { get; set; }
    public decimal TotalGainPercentage { get; set; }
    public decimal TotalDividends { get; set; }
    public List<InvestmentByTypeDto> ByType { get; set; } = new();
}

public class InvestmentByTypeDto
{
    public string Type { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal Percentage { get; set; }
}