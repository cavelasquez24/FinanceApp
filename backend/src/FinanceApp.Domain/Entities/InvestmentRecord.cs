namespace FinanceApp.Domain.Entities;

public class InvestmentRecord : BaseEntity
{
    public Guid InvestmentId { get; set; }
    public DateOnly RecordDate { get; set; }
    public decimal Value { get; set; }
    public decimal Dividends { get; set; } = 0;
    public string? Notes { get; set; }

    // Propiedad de navegación
    public Investment Investment { get; set; } = null!;
}