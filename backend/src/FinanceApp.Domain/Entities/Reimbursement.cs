using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Dinero devuelto por una compra. No es ingreso: revierte parcial o totalmente
/// el coste económico de un gasto y se acredita en una cuenta o en una tarjeta.
/// </summary>
public class Reimbursement : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? ExpenseId { get; set; }
    public ReimbursementDestinationType DestinationType { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Person { get; set; }
    public string? Notes { get; set; }
    public Guid IdempotencyKey { get; set; }

    public User User { get; set; } = null!;
    public Expense? Expense { get; set; }
    public FinancialAccount? Account { get; set; }
    public CreditCard? CreditCard { get; set; }
}
