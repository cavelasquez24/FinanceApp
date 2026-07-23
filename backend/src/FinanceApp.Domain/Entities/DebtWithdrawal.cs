namespace FinanceApp.Domain.Entities;

public class DebtWithdrawal : BaseEntity
{
    public Guid DebtId { get; set; }
    public DateOnly WithdrawalDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }

    public Debt Debt { get; set; } = null!;
}