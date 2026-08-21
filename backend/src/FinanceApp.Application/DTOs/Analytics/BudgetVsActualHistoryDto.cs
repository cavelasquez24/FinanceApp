namespace FinanceApp.Application.DTOs.Analytics;

public class BudgetVsActualHistoryDto
{
    public List<BudgetVsActualPeriodDto> Periods { get; set; } = [];
}

public class BudgetVsActualPeriodDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Budgeted { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance { get; set; }
    public decimal AdherencePct { get; set; }
}
