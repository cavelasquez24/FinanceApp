using FinanceApp.Application.DTOs.Dashboard;

namespace FinanceApp.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task<MonthlyTrendDto> GetMonthlyTrendAsync(
        Guid userId,
        int months,
        CancellationToken cancellationToken = default);

    Task<ExpenseByCategoryChartDto> GetExpensesByCategoryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// v2.0.1 sección 5 — GET /dashboard/cashflow-statement
    /// </summary>
    Task<CashFlowStatementDto> GetCashFlowStatementAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);
}
