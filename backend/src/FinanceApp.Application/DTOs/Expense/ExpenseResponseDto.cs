namespace FinanceApp.Application.DTOs.Expense;

public class ExpenseResponseDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public string? RecurrenceType { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}