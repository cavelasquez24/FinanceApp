using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class NetWorthSnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
    public decimal CashAccounts { get; set; }
    public decimal SavingsAccounts { get; set; }
    public decimal InvestmentPositions { get; set; }
    public decimal DebtLiabilities { get; set; }
    public decimal CreditCardLiabilities { get; set; }
    public SnapshotSource Source { get; set; } = SnapshotSource.Automatic;

    public User User { get; set; } = null!;
}
