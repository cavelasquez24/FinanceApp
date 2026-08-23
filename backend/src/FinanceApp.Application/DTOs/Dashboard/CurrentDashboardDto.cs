namespace FinanceApp.Application.DTOs.Dashboard;

public class CurrentDashboardDto
{
    public DateOnly AsOf => FinancialPosition.AsOf;
    public DateOnly CycleStart { get; set; }
    public DateOnly CycleEnd { get; set; }
    public string CycleLabel { get; set; } = string.Empty;

    public decimal CashBalance => FinancialPosition.Assets.CashAccounts;
    public decimal SavingsBalance => FinancialPosition.Assets.SavingsAccounts;
    public decimal InvestmentBalance => FinancialPosition.Assets.Investments;
    public decimal DebtBalance => FinancialPosition.Liabilities.Total;
    public decimal NetWorth => FinancialPosition.NetWorth;
    public FinancialPositionDto FinancialPosition { get; set; } = new();

    public decimal CycleIncome { get; set; }
    public decimal CycleExpenses { get; set; }
    public decimal CycleReimbursements { get; set; }
    public decimal CycleNetExpenses { get; set; }
    public decimal CycleCashFlow { get; set; }
    public decimal BudgetAvailable { get; set; }
    public decimal CycleSavings { get; set; }
    public decimal CycleInvestments { get; set; }
    public decimal CycleDebtPayments { get; set; }
    public decimal CycleAvailable { get; set; }

    /// <summary>
    /// Porcentaje (0-100) del presupuesto ya consumido, calculado por
    /// BudgetService sobre categorías presupuestadas + rollover. Null si el
    /// usuario no tiene presupuesto configurado para el ciclo actual.
    /// </summary>
    public decimal? PercentageUsed { get; set; }
    public bool? IsOverBudget { get; set; }
    public decimal SuggestedDailyAvailable { get; set; }
    public int DaysRemaining { get; set; }

    /// <summary>
    /// Suma de Min(MonthlyDebitAmount, PendingAmount) de los planes de
    /// reposición Active + AutoDebitEnabled + no pausados del usuario.
    /// Se muestra como compromiso separado — NO se resta de
    /// BudgetAvailable, que sigue siendo únicamente lo que calcula
    /// BudgetService.
    /// </summary>
    public decimal CycleReplenishmentCommitment { get; set; }
}
