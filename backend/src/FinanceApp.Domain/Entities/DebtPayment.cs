namespace FinanceApp.Domain.Entities;

public class DebtPayment : BaseEntity
{
    public Guid DebtId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }   // reduce CurrentBalance
    public decimal InterestAmount { get; set; }     // informativo, default 0
    public string? Notes { get; set; }

    // Propiedad de navegación
    public Debt Debt { get; set; } = null!;
}