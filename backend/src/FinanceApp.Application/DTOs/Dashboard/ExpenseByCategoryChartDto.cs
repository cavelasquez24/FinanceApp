namespace FinanceApp.Application.DTOs.Dashboard;

/// <summary>
/// Gastos por categoría para el gráfico de torta.
/// </summary>
public class ExpenseByCategoryChartDto
{
    public List<CategoryChartItemDto> Categories { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class CategoryChartItemDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}