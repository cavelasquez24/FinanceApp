namespace FinanceApp.Application.DTOs.Analytics;

public class DebtProjectionDto
{
    public decimal TotalOutstanding { get; set; }
    public decimal AvgMonthlyPayment { get; set; }
    public int? EstimatedPayoffMonths { get; set; }
    public DateOnly? EstimatedPayoffDate { get; set; }
    public List<DebtLineProjectionDto> ByDebt { get; set; } = [];
}

public class DebtLineProjectionDto
{
    public Guid DebtId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal AvgMonthlyPayment { get; set; }
    public int? EstimatedPayoffMonths { get; set; }
    public DateOnly? EstimatedPayoffDate { get; set; }
}
