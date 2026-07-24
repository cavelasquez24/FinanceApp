namespace FinanceApp.Application.DTOs.Income;

/// <summary>
/// Datos que llegan cuando el usuario edita un ingreso.
/// Mismos campos que Create — en PUT se reemplazan todos.
/// </summary>
public class IncomeUpdateDto
{
    public Guid CategoryId { get; set; }
    public Guid? AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public DateOnly? AssignedCycleStart { get; set; }
    public string? Source { get; set; }
}