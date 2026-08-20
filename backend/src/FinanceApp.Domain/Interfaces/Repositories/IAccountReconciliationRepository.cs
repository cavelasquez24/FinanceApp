using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IAccountReconciliationRepository : IBaseRepository<AccountReconciliation>
{
    Task<IReadOnlyList<AccountReconciliation>> GetByAccountAsync(
        Guid accountId, Guid userId, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<AccountReconciliation?> GetLastByAccountAsync(
        Guid accountId, Guid userId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetLedgerBalanceAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);
}
