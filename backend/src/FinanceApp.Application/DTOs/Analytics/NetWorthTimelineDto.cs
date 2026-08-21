namespace FinanceApp.Application.DTOs.Analytics;

public class NetWorthTimelineDto
{
    public List<string> Labels { get; set; } = [];
    public List<decimal> NetWorth { get; set; } = [];
    public List<decimal> TotalAssets { get; set; } = [];
    public List<decimal> TotalLiabilities { get; set; } = [];
    public decimal NetWorthChange { get; set; }
    public decimal NetWorthChangePct { get; set; }
}
