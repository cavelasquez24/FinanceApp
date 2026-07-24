using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class IncomeRepository : BaseRepository<Income>, IIncomeRepository
{
    public IncomeRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<Income> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, Guid? categoryId = null,
        DateOnly? startDate = null, DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Incomes
            .Include(i => i.Category)
            .Include(i => i.Account)
            .Where(i => i.UserId == userId && i.DeletedAt == null);

        if (categoryId.HasValue)
            query = query.Where(i => i.CategoryId == categoryId.Value);
        if (startDate.HasValue)
            query = query.Where(i => i.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(i => i.Date <= endDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public override Task<Income?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        _context.Incomes
            .Include(i => i.Category)
            .Include(i => i.Account)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<decimal> GetTotalByUserAndPeriodAsync(
        Guid userId, int month, int year,
        CancellationToken cancellationToken = default) =>
        _context.Incomes
            .Where(i => i.UserId == userId
                && i.Date.Month == month
                && i.Date.Year == year
                && i.DeletedAt == null)
            .SumAsync(i => i.Amount, cancellationToken);

    public Task<decimal> GetTotalByDateRangeAsync(
        Guid userId, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        _context.Incomes
            .Where(i => i.UserId == userId
                && i.Date >= startDate
                && i.Date <= endDate
                && i.DeletedAt == null)
            .SumAsync(i => i.Amount, cancellationToken);

    public Task<decimal> GetTotalByCycleAsync(
        Guid userId, DateOnly cycleStart, DateOnly cycleEnd,
        CancellationToken cancellationToken = default) =>
        _context.Incomes
            .Where(i => i.UserId == userId
                && i.DeletedAt == null
                && (i.AssignedCycleStart == cycleStart
                    || (i.AssignedCycleStart == null
                        && i.Date >= cycleStart
                        && i.Date <= cycleEnd)))
            .SumAsync(i => i.Amount, cancellationToken);
}
