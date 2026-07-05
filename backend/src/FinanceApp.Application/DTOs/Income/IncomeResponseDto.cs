namespace FinanceApp.Application.DTOs.Income;

/// <summary>
/// Datos que retornamos al frontend.
/// Incluye información de la categoría para no hacer
/// una segunda consulta desde el frontend.
/// </summary>
public class IncomeResponseDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public string? Source { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}