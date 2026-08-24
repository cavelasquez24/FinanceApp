using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class AccountTransferRepository
    : BaseRepository<AccountTransfer>, IAccountTransferRepository
{
    public AccountTransferRepository(AppDbContext context) : base(context) { }

    public Task<AccountTransfer?> GetOwnedByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        _context.AccountTransfers
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .FirstOrDefaultAsync(
                t => t.Id == id && t.UserId == userId && t.DeletedAt == null,
                cancellationToken);

    public async Task<IReadOnlyList<AccountTransfer>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _context.AccountTransfers
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t => t.UserId == userId && t.DeletedAt == null)
            .OrderByDescending(t => t.TransferDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<AccountTransfer?> GetByTransferGroupIdAsync(
        Guid userId, Guid transferGroupId, CancellationToken cancellationToken = default) =>
        _context.AccountTransfers.FirstOrDefaultAsync(
            t => t.UserId == userId
                && t.TransferGroupId == transferGroupId
                && t.DeletedAt == null,
            cancellationToken);
}
