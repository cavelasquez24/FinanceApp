namespace FinanceApp.Application.DTOs.Income;

/// <summary>
/// Resumen de ingresos por categoría para el Dashboard.
/// </summary>
public class IncomeSummaryDto
{
    public decimal TotalAmount { get; set; }
    public int TotalCount { get; set; }
    public List<IncomeByCategoryDto> ByCategory { get; set; } = new();
}

public class IncomeByCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}