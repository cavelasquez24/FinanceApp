using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Agregado raíz de una transferencia entre dos cuentas propias del mismo
/// usuario. Representa la intención/resultado de la operación; las dos
/// patas contables viven como AccountTransaction (SourceId = este Id).
/// TransferGroupId permite enlazar una futura reversa (Cancelled) con la
/// transferencia original sin duplicar Id ni romper trazabilidad.
/// </summary>
public class AccountTransfer : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransferDate { get; set; }
    public string? Description { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Completed;
    public Guid TransferGroupId { get; set; }

    public User User { get; set; } = null!;
    public FinancialAccount FromAccount { get; set; } = null!;
    public FinancialAccount ToAccount { get; set; } = null!;
}
