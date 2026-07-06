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
}