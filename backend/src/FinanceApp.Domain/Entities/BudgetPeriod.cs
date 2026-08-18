namespace FinanceApp.Domain.Entities;

public class BudgetPeriod : BaseEntity
{
    public Guid UserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal? TotalLimit { get; set; }
    public string? Notes { get; set; }
    public decimal BudgetRolloverAmount { get; set; }
    public DateOnly? BudgetRolloverDate { get; set; }
    public string? BudgetRolloverNote { get; set; }
    public Guid? BudgetRolloverIdempotencyKey { get; set; }

    // Propiedades de navegación
    public User User { get; set; } = null!;
    public ICollection<BudgetCategory> BudgetCategories { get; set; } = new List<BudgetCategory>();
}