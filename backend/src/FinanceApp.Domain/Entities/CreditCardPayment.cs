namespace FinanceApp.Domain.Entities;

public class CreditCardPayment : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CreditCardId { get; set; }
    public Guid SourceAccountId { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CardBalanceAfter { get; set; }
    public Guid? CommissionExpenseId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string? Notes { get; set; }
    public Guid IdempotencyKey { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    public Guid? VoidIdempotencyKey { get; set; }
    public bool IsVoided => VoidedAt.HasValue;

    public User User { get; set; } = null!;
    public CreditCard CreditCard { get; set; } = null!;
    public FinancialAccount SourceAccount { get; set; } = null!;
    public Expense? CommissionExpense { get; set; }
}
