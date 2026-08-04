using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Models;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface ITagRepository : IBaseRepository<Tag>
{
    Task<IReadOnlyList<Tag>> GetByUserAsync(Guid userId, string? search = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetActiveByIdsAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveAsync(Guid userId, string normalizedName, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task MergeAsync(Tag source, Tag target, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TagExpenseMetric>> GetExpenseReportAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<(int TotalExpenses, int TaggedExpenses)> GetCoverageAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
