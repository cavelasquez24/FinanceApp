using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class AccountReconciliation : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public DateOnly ReconciliationDate { get; set; }
    public decimal ExpectedBalance { get; set; }
    public decimal ActualBalance { get; set; }
    public decimal Difference { get; set; }
    public Guid? AdjustmentTransactionId { get; set; }
    public string? Notes { get; set; }
    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.Reconciled;

    public User User { get; set; } = null!;
    public FinancialAccount Account { get; set; } = null!;
    public AccountTransaction? AdjustmentTransaction { get; set; }
}
