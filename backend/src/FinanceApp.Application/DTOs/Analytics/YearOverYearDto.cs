namespace FinanceApp.Application.DTOs.Analytics;

public class YearOverYearDto
{
    public int Year { get; set; }
    public int PreviousYear { get; set; }
    public List<YoYMonthDto> Months { get; set; } = [];
    public YoYTotalsDto Totals { get; set; } = new();
}

public class YoYMonthDto
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal CurrentIncome { get; set; }
    public decimal CurrentExpenses { get; set; }
    public decimal CurrentNetSavings { get; set; }
    public decimal PrevIncome { get; set; }
    public decimal PrevExpenses { get; set; }
    public decimal PrevNetSavings { get; set; }
}

public class YoYTotalsDto
{
    public decimal IncomeChangeAbs { get; set; }
    public decimal IncomeChangePct { get; set; }
    public decimal ExpensesChangeAbs { get; set; }
    public decimal ExpensesChangePct { get; set; }
    public decimal NetSavingsChangeAbs { get; set; }
    public decimal NetSavingsChangePct { get; set; }
}
