using FinanceApp.Application.DTOs.Dashboard;

namespace FinanceApp.Application.Interfaces;

public interface ICurrentDashboardService
{
    Task<CurrentDashboardDto> GetAsync(
        Guid userId, CancellationToken cancellationToken = default);
}
