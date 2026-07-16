namespace FinanceApp.Application.DTOs.Debt;

public class DebtSummaryDto
{
    public decimal TotalOriginal { get; set; }
    public decimal TotalCurrentBalance { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPaidPercentage { get; set; }
    public List<DebtByTypeDto> ByType { get; set; } = new();
    public List<UpcomingPaymentDto> UpcomingPayments { get; set; } = new();
}

public class DebtByTypeDto
{
    public string Type { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal Percentage { get; set; }
}

public class UpcomingPaymentDto
{
    public Guid DebtId { get; set; }
    public string DebtName { get; set; } = string.Empty;
    public int DueDay { get; set; }
    public decimal? MinimumPayment { get; set; }
}