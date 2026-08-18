namespace FinanceApp.Application.DTOs.Dashboard;

/// <summary>
/// v2.0.1 sección 5 — separa flujo de caja (consumo real) de
/// construcción de patrimonio (ahorro/inversión/pago de deuda).
/// </summary>
public class CashFlowStatementDto
{
    public decimal Income { get; set; }
    public decimal ConsumptionExpenses { get; set; }
    public decimal ReimbursementsReceived { get; set; }
    public decimal NetPersonalExpenses { get; set; }
    public decimal SavingsContributions { get; set; }
    public decimal RestorationContributions { get; set; }
    public decimal NewSavingsContributions { get; set; }
    public decimal InvestmentContributions { get; set; }
    public decimal SavingsWithdrawals { get; set; }
    public decimal DebtPrincipalPaid { get; set; }
    public decimal CashFlowResidual { get; set; }
    public decimal ConsumptionRate { get; set; }
    public decimal WealthBuildingRate { get; set; }
}
