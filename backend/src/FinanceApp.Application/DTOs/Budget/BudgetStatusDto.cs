namespace FinanceApp.Application.DTOs.Budget;

/// <summary>
/// Estado actual del presupuesto — cuánto se ha gastado
/// vs cuánto se tiene permitido gastar por categoría.
/// </summary>
public class BudgetStatusDto
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalLimit { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public bool IsOverBudget { get; set; }
    public List<BudgetCategoryStatusDto> Categories { get; set; } = new();
}

public class BudgetCategoryStatusDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public decimal AmountLimit { get; set; }
    public decimal AmountSpent { get; set; }
    public decimal AmountRemaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public bool IsOverBudget { get; set; }

    /// <summary>
    /// Alerta cuando se supera el 80% del límite.
    /// </summary>
    public bool Alert => PercentageUsed >= 80 && !IsOverBudget;
}