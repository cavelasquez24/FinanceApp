namespace FinanceApp.Application.Services;

public static class CashFlowCalculator
{
    public static decimal CalculateResidual(
        decimal income,
        decimal consumptionExpenses,
        decimal savingsContributions,
        decimal savingsWithdrawals,
        decimal investmentContributions,
        decimal debtPrincipalPaid)
    {
        return income
            + savingsWithdrawals
            - consumptionExpenses
            - savingsContributions
            - investmentContributions
            - debtPrincipalPaid;
    }
}
