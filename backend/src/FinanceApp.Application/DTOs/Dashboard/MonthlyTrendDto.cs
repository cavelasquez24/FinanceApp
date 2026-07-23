namespace FinanceApp.Application.DTOs.Dashboard;

/// <summary>
/// Evolución por ciclo para el gráfico de líneas.
/// Contiene ingreso, gasto y disponible residual real.
/// </summary>
public class MonthlyTrendDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Income { get; set; } = new();
    public List<decimal> Expenses { get; set; } = new();
    public List<decimal> Residual { get; set; } = new();
}