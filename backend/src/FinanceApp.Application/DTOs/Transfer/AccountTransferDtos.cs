namespace FinanceApp.Application.DTOs.Transfer;

public class AccountTransferCreateDto
{
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransferDate { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Opcional. Si el cliente reintenta el envío (p.ej. timeout de red),
    /// reenvía el mismo valor para que CreateAsync detecte el duplicado
    /// vía GetByTransferGroupIdAsync y devuelva el existente sin duplicar.
    /// Si se omite, el servicio genera uno nuevo (Guid.NewGuid()).
    /// </summary>
    public Guid? TransferGroupId { get; set; }
}

public class AccountTransferDto
{
    public Guid Id { get; set; }
    public Guid FromAccountId { get; set; }
    public string FromAccountName { get; set; } = string.Empty;
    public Guid ToAccountId { get; set; }
    public string ToAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly TransferDate { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid TransferGroupId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Versión liviana de AccountTransferDto para listados de historial
/// (GetByUserIdAsync). Omite TransferGroupId: el historial no necesita
/// agrupar visualmente, solo el detalle/cancelación sí.
/// </summary>
public class AccountTransferSummaryDto
{
    public Guid Id { get; set; }
    public Guid FromAccountId { get; set; }
    public string FromAccountName { get; set; } = string.Empty;
    public Guid ToAccountId { get; set; }
    public string ToAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly TransferDate { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Resultado de CreateAsync: la transferencia creada (o la existente si
/// fue una llamada idempotente repetida) más la advertencia de saldo,
/// que solo tiene sentido en el momento de la creación.
/// </summary>
public class AccountTransferCreateResultDto
{
    public AccountTransferDto Transfer { get; set; } = null!;
    public bool InsufficientFundsWarning { get; set; }
}
