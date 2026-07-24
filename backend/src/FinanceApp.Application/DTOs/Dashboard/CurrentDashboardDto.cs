namespace FinanceApp.Application.DTOs.Dashboard;

public class CurrentDashboardDto
{
    public DateOnly AsOf { get; set; }
    public DateOnly CycleStart { get; set; }
    public DateOnly CycleEnd { get; set; }
    public string CycleLabel { get; set; } = string.Empty;

    public decimal CashBalance { get; set; }
    public decimal SavingsBalance { get; set; }
    public decimal InvestmentBalance { get; set; }
    public decimal DebtBalance { get; set; }
    public decimal NetWorth { get; set; }

    public decimal CycleIncome { get; set; }
    public decimal CycleExpenses { get; set; }
    public decimal CycleSavings { get; set; }
    public decimal CycleInvestments { get; set; }
    public decimal CycleDebtPayments { get; set; }
    public decimal CycleAvailable { get; set; }
    public decimal SuggestedDailyAvailable { get; set; }
    public int DaysRemaining { get; set; }
}
