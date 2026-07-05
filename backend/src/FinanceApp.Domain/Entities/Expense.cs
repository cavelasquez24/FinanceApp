using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Expense : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Solo tiene valor si IsRecurring es true.
    /// </summary>
    public RecurrenceType? RecurrenceType { get; set; }

    public string? Notes { get; set; }

    // Propiedades de navegación
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}