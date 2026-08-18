namespace FinanceApp.Application.DTOs.Expense;

public class ExpenseCreateDto
{
    public Guid CategoryId { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Merchant { get; set; }
    public List<Guid> TagIds { get; set; } = new();
    public DateOnly Date { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public bool IsRecurring { get; set; } = false;
    public string? RecurrenceType { get; set; }
    public string? Notes { get; set; }
}