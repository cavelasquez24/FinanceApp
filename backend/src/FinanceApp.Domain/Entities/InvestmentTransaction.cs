using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class InvestmentTransaction : BaseEntity
{
    public Guid InvestmentId { get; set; }
    public InvestmentTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public bool IsHistorical { get; set; } = false;
    public Guid? GroupId { get; set; }
    public Guid? ReversalOf { get; set; }
    public string? Notes { get; set; }

    public Investment Investment { get; set; } = null!;
}
