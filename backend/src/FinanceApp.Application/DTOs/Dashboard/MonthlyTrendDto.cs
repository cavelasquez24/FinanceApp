namespace FinanceApp.Application.DTOs.Dashboard;

/// <summary>
/// Evolución mensual para el gráfico de líneas.
/// Contiene los últimos N meses de ingresos, gastos y ahorro.
/// </summary>
public class MonthlyTrendDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Income { get; set; } = new();
    public List<decimal> Expenses { get; set; } = new();
    public List<decimal> Savings { get; set; } = new();
}