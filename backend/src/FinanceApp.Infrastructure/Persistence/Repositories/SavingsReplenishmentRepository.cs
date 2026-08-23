using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class SavingsReplenishmentRepository
    : BaseRepository<SavingsReplenishment>, ISavingsReplenishmentRepository
{
    public SavingsReplenishmentRepository(AppDbContext context) : base(context) { }

    public Task<SavingsReplenishment?> GetOwnedByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        _context.SavingsReplenishments
            .Include(r => r.SavingsGoal)
            .Include(r => r.SourceAccount)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && r.DeletedAt == null, cancellationToken);

    public async Task<IReadOnlyList<SavingsReplenishment>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _context.SavingsReplenishments
            .Include(r => r.SavingsGoal)
            .Include(r => r.SourceAccount)
            .Where(r => r.UserId == userId && r.DeletedAt == null)
            .OrderBy(r => r.Status == ReplenishmentStatus.Active ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SavingsReplenishment>> GetByGoalIdAsync(
        Guid goalId, Guid userId, CancellationToken cancellationToken = default) =>
        await _context.SavingsReplenishments
            .Include(r => r.SourceAccount)
            .Where(r => r.SavingsGoalId == goalId && r.UserId == userId && r.DeletedAt == null)
            .OrderBy(r => r.Status == ReplenishmentStatus.Active ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SavingsReplenishment>> GetActiveForAutoDebitAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _context.SavingsReplenishments
            .Include(r => r.SavingsGoal)
            .Include(r => r.SourceAccount)
            .Where(r => r.UserId == userId
                && r.Status == ReplenishmentStatus.Active
                && r.AutoDebitEnabled
                && !r.IsPaused
                && r.DeletedAt == null)
            .ToListAsync(cancellationToken);
}
