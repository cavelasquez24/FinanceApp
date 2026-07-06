namespace FinanceApp.Application.DTOs.Budget;

public class BudgetUpdateDto
{
    public decimal? TotalLimit { get; set; }
    public string? Notes { get; set; }
    public List<BudgetCategoryCreateDto> Categories { get; set; } = new();
}