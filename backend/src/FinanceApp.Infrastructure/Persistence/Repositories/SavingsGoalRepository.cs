using FinanceApp.Domain.Entities;
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
}