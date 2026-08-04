namespace FinanceApp.Domain.Entities;

public class ExpenseTag
{
    public Guid ExpenseId { get; set; }
    public Guid TagId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Expense Expense { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
