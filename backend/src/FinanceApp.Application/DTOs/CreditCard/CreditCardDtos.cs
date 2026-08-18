namespace FinanceApp.Application.DTOs.CreditCard;

public class CreditCardCreateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public DateOnly OpeningDate { get; set; }
    public decimal? CreditLimit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public string? Notes { get; set; }
}

public class CreditCardUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal? CreditLimit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public class CreditCardResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? AvailableCredit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreditCardPaymentCreateDto
{
    public Guid SourceAccountId { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public Guid? CommissionCategoryId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string? Notes { get; set; }
    public Guid IdempotencyKey { get; set; }
}

public class CreditCardPaymentVoidDto
{
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid IdempotencyKey { get; set; }
}

public class CreditCardPaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid CreditCardId { get; set; }
    public Guid SourceAccountId { get; set; }
    public string SourceAccountName { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public Guid? CommissionExpenseId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string? Notes { get; set; }
    public Guid IdempotencyKey { get; set; }
    public decimal CardBalanceAfter { get; set; }
    public bool IsVoided { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    public Guid? VoidIdempotencyKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreditCardChargeCreateDto
{
    public string Type { get; set; } = "interest";
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public Guid IdempotencyKey { get; set; }
}

public class CreditCardTransactionResponseDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
