using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class FinancialAccount : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public FinancialAccountType Type { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public ICollection<AccountTransaction> Transactions { get; set; } = new List<AccountTransaction>();
}
