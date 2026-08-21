namespace FinanceApp.Application.DTOs.Analytics;

public class ExpenseIntelligenceDto
{
    public List<TopMerchantDto> TopMerchants { get; set; } = [];
    public List<RecurringExpenseDto> RecurringExpenses { get; set; } = [];
    public List<CategoryDriftDto> CategoryDrift { get; set; } = [];
}

public class TopMerchantDto
{
    public string Merchant { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class RecurringExpenseDto
{
    public Guid ExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string RecurrenceType { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal AnnualImpact { get; set; }
}

public class CategoryDriftDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal CurrentAmount { get; set; }
    public decimal PreviousAmount { get; set; }
    public decimal DriftAmount { get; set; }
    public decimal DriftPct { get; set; }
}
