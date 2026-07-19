using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class SavingsGoalRepository : BaseRepository<SavingsGoal>, ISavingsGoalRepository
{
    public SavingsGoalRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<SavingsGoal>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SavingsGoals
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalSavedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SavingsGoals
            .Where(s => s.UserId == userId
                     && !s.IsCompleted
                     && s.DeletedAt == null)
            .SumAsync(s => s.CurrentAmount, cancellationToken);
    }

    // Mismo fix aplicado en DebtRepository: _dbSet.Update() marca hijos
    // nuevos adjuntos vía navegación (Contributions/Withdrawals) como
    // Modified en vez de Added, por el Guid.NewGuid() en BaseEntity.Id.
    // Saltamos _dbSet.Update() y dejamos que el change tracker de EF Core
    // detecte los cambios por sí mismo.
    public override async Task<SavingsGoal> UpdateAsync(
        SavingsGoal entity, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    // ---- v2.0.1 — historial de movimientos ----

    public async Task<SavingsGoal?> GetByIdWithHistoryAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SavingsGoals
            .Include(s => s.Contributions.Where(c => c.DeletedAt == null))
            .Include(s => s.Withdrawals.Where(w => w.DeletedAt == null))
            .FirstOrDefaultAsync(
                s => s.Id == id && s.UserId == userId && s.DeletedAt == null,
                cancellationToken);
    }

    public async Task AddContributionAsync(
        SavingsGoalContribution contribution,
        CancellationToken cancellationToken = default)
    {
        await _context.SavingsGoalContributions.AddAsync(contribution, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddWithdrawalAsync(
        SavingsGoalWithdrawal withdrawal,
        CancellationToken cancellationToken = default)
    {
        await _context.SavingsGoalWithdrawals.AddAsync(withdrawal, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalContributionsByDateRangeAsync(
        Guid userId, DateOnly start, DateOnly end,
        CancellationToken cancellationToken = default)
    {
        return await _context.SavingsGoalContributions
            .Where(c => c.SavingsGoal.UserId == userId
                && c.ContributionDate >= start && c.ContributionDate <= end
                && c.DeletedAt == null)
            .SumAsync(c => c.Amount, cancellationToken);
    }

    public async Task<decimal> GetTotalConsumedWithdrawalsByDateRangeAsync(
        Guid userId, DateOnly start, DateOnly end,
        CancellationToken cancellationToken = default)
    {
        return await _context.SavingsGoalWithdrawals
            .Where(w => w.SavingsGoal.UserId == userId
                && w.Reason == SavingsWithdrawalReason.Consumed
                && w.WithdrawalDate >= start && w.WithdrawalDate <= end
                && w.DeletedAt == null)
            .SumAsync(w => w.Amount, cancellationToken);
    }
}
