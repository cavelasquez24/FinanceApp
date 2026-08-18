namespace FinanceApp.Application.DTOs.Account;

public class FinancialAccountResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public bool IsDefault { get; set; }
    public decimal? OpeningBalance { get; set; }
    public DateOnly? OpeningDate { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
}

public class FinancialAccountCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "cash";
    public decimal OpeningBalance { get; set; }
    public bool IsDefault { get; set; }
    public DateOnly? OpeningDate { get; set; }
}

public class FinancialAccountUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AccountTransactionResponseDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? TransferId { get; set; }
}

/// <summary>
/// Una clave de idempotencia identifica de forma estable una transferencia.
/// El cliente debe reutilizarla si necesita reintentar la misma solicitud.
/// </summary>
public class AccountTransferCreateDto
{
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public Guid IdempotencyKey { get; set; }
}

public class AccountTransferResponseDto
{
    public Guid TransferId { get; set; }
    public Guid FromAccountId { get; set; }
    public string FromAccountName { get; set; } = string.Empty;
    public Guid ToAccountId { get; set; }
    public string ToAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
}
