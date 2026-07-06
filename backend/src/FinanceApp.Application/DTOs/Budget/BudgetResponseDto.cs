namespace FinanceApp.Application.DTOs.Budget;

public class BudgetResponseDto
{
    public Guid Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal? TotalLimit { get; set; }
    public string? Notes { get; set; }
    public List<BudgetCategoryResponseDto> Categories { get; set; } = new();
}

public class BudgetCategoryResponseDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public decimal AmountLimit { get; set; }
}