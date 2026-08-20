using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class AccountReconciliationRepository
    : BaseRepository<AccountReconciliation>, IAccountReconciliationRepository
{
    public AccountReconciliationRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AccountReconciliation>> GetByAccountAsync(
        Guid accountId, Guid userId, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        await _context.AccountReconciliations
            .Where(r => r.AccountId == accountId && r.UserId == userId && r.DeletedAt == null)
            .OrderByDescending(r => r.ReconciliationDate)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<AccountReconciliation?> GetLastByAccountAsync(
        Guid accountId, Guid userId,
        CancellationToken cancellationToken = default) =>
        _context.AccountReconciliations
            .Where(r => r.AccountId == accountId && r.UserId == userId && r.DeletedAt == null)
            .OrderByDescending(r => r.ReconciliationDate)
            .ThenByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<decimal> GetLedgerBalanceAsync(
        Guid accountId,
        CancellationToken cancellationToken = default) =>
        await _context.AccountTransactions
            .Where(t => t.AccountId == accountId && t.DeletedAt == null)
            .SumAsync(t => t.Amount, cancellationToken);
}
