using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IFinancialAccountRepository : IBaseRepository<FinancialAccount>
{
    Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<FinancialAccount?> GetDefaultAsync(
        Guid userId, FinancialAccountType type,
        CancellationToken cancellationToken = default);

    Task<AccountTransaction?> GetTransactionBySourceAsync(
        Guid userId, string sourceType, Guid sourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountTransaction>> GetRecentTransactionsAsync(
        Guid userId, int count,
        CancellationToken cancellationToken = default);

    Task SaveTransactionAsync(
        AccountTransaction transaction,
        CancellationToken cancellationToken = default);

    Task DeleteTransactionAsync(
        AccountTransaction transaction,
        CancellationToken cancellationToken = default);
}
