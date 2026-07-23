using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Debt : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DebtType Type { get; set; }
    public string? Creditor { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal? InterestRate { get; set; }       // % anual, opcional
    public decimal? MinimumPayment { get; set; }
    public int? DueDay { get; set; }                 // día del mes 1-31
    public DateOnly StartDate { get; set; }
    public DateOnly? TargetPayoffDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public Guid? LinkedSavingsGoalId { get; set; }
    public ICollection<DebtWithdrawal> Withdrawals { get; set; } = new List<DebtWithdrawal>();


    // Propiedades de navegación
    public User User { get; set; } = null!;
    public ICollection<DebtPayment> Payments { get; set; } = new List<DebtPayment>();

    // Calculadas en memoria, no BD
    public decimal AmountPaid => OriginalAmount - CurrentBalance;
    public decimal PaidPercentage => OriginalAmount > 0
        ? Math.Min(Math.Round((AmountPaid / OriginalAmount) * 100, 2), 100)
        : 0;
    public bool IsPaidOff => CurrentBalance <= 0;
}