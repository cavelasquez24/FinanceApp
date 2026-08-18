using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Movimiento firmado del pasivo. Positivo aumenta la deuda y negativo la reduce.
/// SourceType + SourceId permite sincronizar una compra sin duplicarla.
/// </summary>
public class CreditCardTransaction : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CreditCardId { get; set; }
    public CreditCardTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }

    public User User { get; set; } = null!;
    public CreditCard CreditCard { get; set; } = null!;
}
