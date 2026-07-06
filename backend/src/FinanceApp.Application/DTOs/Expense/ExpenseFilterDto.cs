namespace FinanceApp.Application.DTOs.Expense;

public class ExpenseFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? CategoryId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? Search { get; set; }
    public string? PaymentMethod { get; set; }
    public bool? IsRecurring { get; set; }
    public string SortBy { get; set; } = "date";
    public string SortDirection { get; set; } = "desc";
}