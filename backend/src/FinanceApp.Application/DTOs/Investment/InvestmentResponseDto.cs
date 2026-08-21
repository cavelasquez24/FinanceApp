namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public string? Broker { get; set; }

    // Nombre semántico correcto: capital acumulado por aportes del usuario.
    public decimal ContributedCapital { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal UnrealizedGainLoss { get; set; }
    public decimal UnrealizedGainLossPercentage { get; set; }

    // Aliases de compatibilidad — eliminar cuando el frontend migre a los campos anteriores.
    [Obsolete("Usar ContributedCapital")]
    public decimal InitialAmount { get; set; }
    [Obsolete("Usar UnrealizedGainLoss")]
    public decimal GainLoss { get; set; }
    [Obsolete("Usar UnrealizedGainLossPercentage")]
    public decimal GainLossPercentage { get; set; }

    public DateOnly PurchaseDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
