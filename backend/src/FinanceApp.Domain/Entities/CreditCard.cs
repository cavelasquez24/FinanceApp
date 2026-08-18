namespace FinanceApp.Domain.Entities;

public class CreditCard : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal? CreditLimit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<CreditCardTransaction> Transactions { get; set; } = new List<CreditCardTransaction>();
    public ICollection<CreditCardPayment> Payments { get; set; } = new List<CreditCardPayment>();
    public ICollection<Reimbursement> Reimbursements { get; set; } = new List<Reimbursement>();
}
