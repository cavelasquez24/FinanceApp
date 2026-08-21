using FinanceApp.Application.DTOs.Analytics;

namespace FinanceApp.Application.Interfaces;

public interface IAnalyticsService
{
    Task<NetWorthTimelineDto> GetNetWorthTimelineAsync(Guid userId, int months, CancellationToken ct = default);
    Task<FinancialHealthScoreDto> GetFinancialHealthScoreAsync(Guid userId, int month, int year, CancellationToken ct = default);
    Task<ExpenseIntelligenceDto> GetExpenseIntelligenceAsync(Guid userId, int month, int year, CancellationToken ct = default);
    Task<DebtProjectionDto> GetDebtProjectionAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<SavingsGoalEtaDto>> GetSavingsGoalEtaAsync(Guid userId, CancellationToken ct = default);
    Task<YearOverYearDto> GetYearOverYearAsync(Guid userId, int year, CancellationToken ct = default);
    Task<BudgetVsActualHistoryDto> GetBudgetVsActualHistoryAsync(Guid userId, int months, CancellationToken ct = default);
}
