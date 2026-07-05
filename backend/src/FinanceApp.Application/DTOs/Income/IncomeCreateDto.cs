namespace FinanceApp.Application.DTOs.Income;

/// <summary>
/// Datos que llegan cuando el usuario crea un ingreso.
/// </summary>
public class IncomeCreateDto
{
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public string? Source { get; set; }
}