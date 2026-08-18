namespace FinanceApp.Application.DTOs.Budget;

public class BudgetCreateDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal? TotalLimit { get; set; }
    public string? Notes { get; set; }
    public decimal BudgetRolloverAmount { get; set; }
    public DateOnly? BudgetRolloverDate { get; set; }
    public string? BudgetRolloverNote { get; set; }
    public Guid? BudgetRolloverIdempotencyKey { get; set; }
    public List<BudgetCategoryCreateDto> Categories { get; set; } = new();
}

public class BudgetCategoryCreateDto
{
    public Guid CategoryId { get; set; }
    public decimal AmountLimit { get; set; }
}