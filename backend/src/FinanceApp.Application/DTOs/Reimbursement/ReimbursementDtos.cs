namespace FinanceApp.Application.DTOs.Reimbursement;

public class ReimbursementCreateDto
{
    public Guid? ExpenseId { get; set; }
    public string DestinationType { get; set; } = "account";
    public Guid? AccountId { get; set; }
    public Guid? CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Person { get; set; }
    public string? Notes { get; set; }
    public Guid IdempotencyKey { get; set; }
}

public class ReimbursementUpdateDto : ReimbursementCreateDto { }

public class ReimbursementResponseDto
{
    public Guid Id { get; set; }
    public Guid? ExpenseId { get; set; }
    public string? ExpenseDescription { get; set; }
    public string DestinationType { get; set; } = string.Empty;
    public Guid? AccountId { get; set; }
    public string? AccountName { get; set; }
    public Guid? CreditCardId { get; set; }
    public string? CreditCardName { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Person { get; set; }
    public string? Notes { get; set; }
    public Guid IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ReimbursementSummaryDto
{
    public decimal GrossExpenses { get; set; }
    public decimal ReimbursementsReceived { get; set; }
    public decimal NetPersonalExpenses { get; set; }
}
