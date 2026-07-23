namespace FinanceApp.Application.DTOs.Dashboard;

/// <summary>
/// v2.0.1 sección 5 — separa flujo de caja (consumo real) de
/// construcción de patrimonio (ahorro/inversión/pago de deuda).
/// </summary>
public class CashFlowStatementDto
{
    public decimal Income { get; set; }
    public decimal ConsumptionExpenses { get; set; }       // = Expenses reales, sin contaminar
    public decimal SavingsContributions { get; set; }
    public decimal InvestmentContributions { get; set; }
    public decimal SavingsWithdrawals { get; set; }
    public decimal DebtPrincipalPaid { get; set; }
    public decimal CashFlowResidual { get; set; }           // income + withdrawals - expenses - savings - investments - debt principal
    public decimal ConsumptionRate { get; set; }             // consumptionExpenses / income
    public decimal WealthBuildingRate { get; set; }          // (savingsContributions + investmentContributions + debtPrincipalPaid) / income
}
