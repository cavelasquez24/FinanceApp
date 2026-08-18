namespace FinanceApp.Application.DTOs.Dashboard;

/// <summary>
/// Foto canónica y auditable del patrimonio del usuario.
/// Solo representa saldos actuales; no mezcla métricas de presupuesto o flujo de caja.
/// </summary>
public class FinancialPositionDto
{
    public DateOnly AsOf { get; set; }
    public string ValuationBasis { get; set; } = "current_snapshot";
    public bool HistoricalSnapshotsSupported { get; set; }
    public FinancialPositionAssetsDto Assets { get; set; } = new();
    public FinancialPositionLiabilitiesDto Liabilities { get; set; } = new();
    public decimal SavingsGoalAllocations { get; set; }
    public decimal NetWorth { get; set; }
    public List<FinancialPositionWarningDto> Warnings { get; set; } = [];
}

public class FinancialPositionAssetsDto
{
    public decimal CashAccounts { get; set; }
    public decimal SavingsAccounts { get; set; }
    public decimal AccountOpeningBalances { get; set; }
    public decimal AccountAdjustments { get; set; }
    public decimal InvestmentPositions { get; set; }
    public decimal InvestmentCostBasis { get; set; }
    public decimal InvestmentUnrealizedGainLoss { get; set; }

    /// <summary>
    /// Saldo de la cuenta agregada de inversión. Es un dato diagnóstico y no se
    /// suma directamente, porque normalmente refleja las mismas posiciones.
    /// </summary>
    public decimal InvestmentAccountBalance { get; set; }

    /// <summary>
    /// Exceso positivo de la cuenta agregada sobre las posiciones. Se interpreta
    /// como efectivo aún no invertido y sí forma parte de los activos.
    /// </summary>
    public decimal UninvestedInvestmentCash { get; set; }

    public decimal Investments { get; set; }
    public decimal CreditCardCredits { get; set; }
    public decimal Total { get; set; }
}

public class FinancialPositionLiabilitiesDto
{
    public decimal Debts { get; set; }
    public decimal CreditCards { get; set; }
    public decimal Total { get; set; }
}

public class FinancialPositionWarningDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
