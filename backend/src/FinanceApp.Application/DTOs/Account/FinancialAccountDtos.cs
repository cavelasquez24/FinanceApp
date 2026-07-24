namespace FinanceApp.Application.DTOs.Account;

public class FinancialAccountResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
}

public class FinancialAccountCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "cash";
    public decimal OpeningBalance { get; set; }
    public bool IsDefault { get; set; }
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
}
