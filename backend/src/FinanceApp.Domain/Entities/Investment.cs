using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Investment : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public InvestmentType Type { get; set; }
    public string? Ticker { get; set; }
    public string? Broker { get; set; }
    public decimal InitialAmount { get; set; }
    public decimal CurrentValue { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Propiedades de navegación
    public User User { get; set; } = null!;
    public ICollection<InvestmentRecord> Records { get; set; } = new List<InvestmentRecord>();

    // v2.0.1 — historial de aportes de caja (distinto de Records/valuation)
    public ICollection<InvestmentContribution> Contributions { get; set; } = new List<InvestmentContribution>();

    // Calculadas en memoria no BD
    public decimal GainLoss => CurrentValue - InitialAmount;
    public decimal GainLossPercentage => InitialAmount > 0
        ? (GainLoss / InitialAmount) * 100
        : 0;
}
