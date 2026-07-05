namespace FinanceApp.Domain.Entities;

public class BudgetCategory : BaseEntity
{
    public Guid BudgetPeriodId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal AmountLimit { get; set; }

    // Propiedades de navegación
    public BudgetPeriod BudgetPeriod { get; set; } = null!;
    public Category Category { get; set; } = null!;
}