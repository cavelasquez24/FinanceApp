using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface INetWorthSnapshotRepository
{
    Task<NetWorthSnapshot?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<NetWorthSnapshot>> GetRangeAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task UpsertAsync(NetWorthSnapshot snapshot, CancellationToken ct = default);
}
