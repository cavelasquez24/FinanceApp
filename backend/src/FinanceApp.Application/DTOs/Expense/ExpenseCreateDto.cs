namespace FinanceApp.Application.DTOs.Expense;

public class ExpenseCreateDto
{
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public bool IsRecurring { get; set; } = false;
    public string? RecurrenceType { get; set; }
    public string? Notes { get; set; }
}