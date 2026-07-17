using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class ExpenseRepository : BaseRepository<Expense>, IExpenseRepository
{
    public ExpenseRepository(AppDbContext context) : base(context) { }

    public override async Task<Expense?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Expense> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? categoryId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == userId && e.DeletedAt == null);

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        if (startDate.HasValue)
            query = query.Where(e => e.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Date <= endDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<decimal> GetTotalByUserAndPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .Where(e => e.UserId == userId
                     && e.Date.Month == month
                     && e.Date.Year == year
                     && e.DeletedAt == null)
            .SumAsync(e => e.Amount, cancellationToken);
    }

    public async Task<IEnumerable<(Guid CategoryId, string CategoryName, string CategoryColor, decimal Total)>>
        GetByCategoryAsync(
            Guid userId,
            int month,
            int year,
            CancellationToken cancellationToken = default)
    {
        var results = await _context.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == userId
                     && e.Date.Month == month
                     && e.Date.Year == year
                     && e.DeletedAt == null)
            .GroupBy(e => new
            {
                e.CategoryId,
                e.Category.Name,
                e.Category.Color
            })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                g.Key.Color,
                Total = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        return results.Select(r => (r.CategoryId, r.Name, r.Color, r.Total));
    }

    public async Task<decimal> GetTotalByDateRangeAsync(
    Guid userId, DateOnly startDate, DateOnly endDate,
    CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .Where(e => e.UserId == userId
                     && e.Date >= startDate
                     && e.Date <= endDate
                     && e.DeletedAt == null)
            .SumAsync(e => e.Amount, cancellationToken);
    }

    public async Task<IEnumerable<(Guid CategoryId, string CategoryName, string CategoryColor, decimal Total)>>
        GetByCategoryByDateRangeAsync(
            Guid userId, DateOnly startDate, DateOnly endDate,
            CancellationToken cancellationToken = default)
    {
        var results = await _context.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == userId
                     && e.Date >= startDate
                     && e.Date <= endDate
                     && e.DeletedAt == null)
            .GroupBy(e => new { e.CategoryId, e.Category.Name, e.Category.Color })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                g.Key.Color,
                Total = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        return results.Select(r => (r.CategoryId, r.Name, r.Color, r.Total));
    }
}