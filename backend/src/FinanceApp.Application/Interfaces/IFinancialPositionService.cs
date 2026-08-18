using FinanceApp.Application.DTOs.Dashboard;

namespace FinanceApp.Application.Interfaces;

public interface IFinancialPositionService
{
    Task<FinancialPositionDto> GetAsync(
        Guid userId,
        DateOnly? asOf = null,
        CancellationToken cancellationToken = default);
}
