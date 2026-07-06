using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class BudgetRepository : BaseRepository<BudgetPeriod>, IBudgetRepository
{
    public BudgetRepository(AppDbContext context) : base(context) { }

    public override async Task<BudgetPeriod?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BudgetPeriods
            .Include(b => b.BudgetCategories)
                .ThenInclude(bc => bc.Category)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<BudgetPeriod?> GetByUserAndPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.BudgetPeriods
            .Include(b => b.BudgetCategories)
                .ThenInclude(bc => bc.Category)
            .FirstOrDefaultAsync(b => b.UserId == userId
                                   && b.Month == month
                                   && b.Year == year,
                cancellationToken);
    }

    public async Task<IEnumerable<BudgetPeriod>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.BudgetPeriods
            .Include(b => b.BudgetCategories)
                .ThenInclude(bc => bc.Category)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ToListAsync(cancellationToken);
    }
}