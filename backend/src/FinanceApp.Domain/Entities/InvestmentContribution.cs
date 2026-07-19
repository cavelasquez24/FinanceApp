namespace FinanceApp.Domain.Entities;

/// <summary>
/// Registro histórico de dinero aportado por el usuario a una Investment
/// (aumenta el costo base). Distinto de InvestmentRecord, que es un
/// snapshot de valor de mercado (revalorización, no aporte de caja).
/// </summary>
public class InvestmentContribution : BaseEntity
{
    public Guid InvestmentId { get; set; }
    public DateOnly ContributionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }

    // Propiedad de navegación
    public Investment Investment { get; set; } = null!;
}
