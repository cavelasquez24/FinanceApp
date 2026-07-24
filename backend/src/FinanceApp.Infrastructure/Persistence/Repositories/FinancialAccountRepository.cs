using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class FinancialAccountRepository
    : BaseRepository<FinancialAccount>, IFinancialAccountRepository
{
    public FinancialAccountRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _context.FinancialAccounts
            .Where(a => a.UserId == userId && a.DeletedAt == null)
            .OrderBy(a => a.Type)
            .ThenByDescending(a => a.IsDefault)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public Task<FinancialAccount?> GetDefaultAsync(
        Guid userId, FinancialAccountType type,
        CancellationToken cancellationToken = default) =>
        _context.FinancialAccounts.FirstOrDefaultAsync(
            a => a.UserId == userId
                && a.Type == type
                && a.IsDefault
                && a.IsActive
                && a.DeletedAt == null,
            cancellationToken);

    public Task<AccountTransaction?> GetTransactionBySourceAsync(
        Guid userId, string sourceType, Guid sourceId,
        CancellationToken cancellationToken = default) =>
        _context.AccountTransactions.FirstOrDefaultAsync(
            t => t.UserId == userId
                && t.SourceType == sourceType
                && t.SourceId == sourceId
                && t.DeletedAt == null,
            cancellationToken);

    public async Task<IReadOnlyList<AccountTransaction>> GetRecentTransactionsAsync(
        Guid userId, int count,
        CancellationToken cancellationToken = default) =>
        await _context.AccountTransactions
            .Include(t => t.Account)
            .Where(t => t.UserId == userId && t.DeletedAt == null)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task SaveTransactionAsync(
        AccountTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(transaction).State == EntityState.Detached)
            await _context.AccountTransactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTransactionAsync(
        AccountTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        transaction.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
