using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IEmergencyFundRestorationRepository : IBaseRepository<EmergencyFundRestoration>
{
    Task<EmergencyFundRestoration?> GetOwnedByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmergencyFundRestoration>> GetByGoalAsync(Guid goalId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetDueIdsAsync(
        Guid userId, DateOnly asOfDate, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalOutstandingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<decimal> GetRestoredByDateRangeAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
}
