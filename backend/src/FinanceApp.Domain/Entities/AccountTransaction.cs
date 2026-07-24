namespace FinanceApp.Domain.Entities;

/// <summary>
/// Movimiento firmado de una cuenta. Positivo incrementa el saldo y
/// negativo lo reduce. SourceType + SourceId permite sincronizar ediciones
/// y eliminaciones del módulo que originó el movimiento.
/// </summary>
public class AccountTransaction : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }

    public User User { get; set; } = null!;
    public FinancialAccount Account { get; set; } = null!;
}
