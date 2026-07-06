namespace FinanceApp.Application.DTOs.Expense;

public class ExpenseSummaryDto
{
    public decimal TotalAmount { get; set; }
    public int TotalCount { get; set; }
    public List<ExpenseByCategoryDto> ByCategory { get; set; } = new();
}

public class ExpenseByCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}