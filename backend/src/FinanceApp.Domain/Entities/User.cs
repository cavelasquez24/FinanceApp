namespace FinanceApp.Domain.Entities;

/// <summary>
/// Usuario del sistema.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash generado por BCrypt.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Código ISO 4217: USD, EUR, COP, PEN, MXN, etc.
    /// </summary>
    public string CurrencyCode { get; set; } = "USD";

    // EF Core las usa para hacer JOIN entre tablas.
    public ICollection<Income> Incomes { get; set; } = new List<Income>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<BudgetPeriod> BudgetPeriods { get; set; } = new List<BudgetPeriod>();
    public ICollection<Investment> Investments { get; set; } = new List<Investment>();
    public ICollection<SavingsGoal> SavingsGoals { get; set; } = new List<SavingsGoal>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Calculada en memoria
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";
}