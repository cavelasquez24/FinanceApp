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
    public decimal SuggestedDailyAvailable { get; set; }
    public int DaysRemaining { get; set; }
}
