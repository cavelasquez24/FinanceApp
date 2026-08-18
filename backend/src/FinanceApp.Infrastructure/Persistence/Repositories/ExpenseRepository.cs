using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Domain.Models;
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
            .Include(e => e.Account)
            .Include(e => e.ExpenseTags)
                .ThenInclude(et => et.Tag)
            .Include(e => e.CreditCard)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task<Expense?> GetByIdempotencyKeyAsync(
        Guid userId, Guid idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.Account)
            .Include(e => e.CreditCard)
            .Include(e => e.ExpenseTags)
                .ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(
                e => e.UserId == userId && e.IdempotencyKey == idempotencyKey
                    && e.DeletedAt == null,
                cancellationToken);

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
            .Include(e => e.Account)
            .Include(e => e.CreditCard)
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
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Expense> Items, int TotalCount)> GetFilteredAsync(
        Guid userId, ExpenseQueryOptions options, CancellationToken cancellationToken = default)
    {
        var query = _context.Expenses
            .Where(e => e.UserId == userId && e.DeletedAt == null);

        if (options.CategoryId.HasValue)
            query = query.Where(e => e.CategoryId == options.CategoryId.Value);
        if (options.StartDate.HasValue)
            query = query.Where(e => e.Date >= options.StartDate.Value);
        if (options.EndDate.HasValue)
            query = query.Where(e => e.Date <= options.EndDate.Value);
        if (options.MinAmount.HasValue)
            query = query.Where(e => e.Amount >= options.MinAmount.Value);
        if (options.MaxAmount.HasValue)
            query = query.Where(e => e.Amount <= options.MaxAmount.Value);
        if (options.PaymentMethod.HasValue)
            query = query.Where(e => e.PaymentMethod == options.PaymentMethod.Value);
        if (options.IsRecurring.HasValue)
            query = query.Where(e => e.IsRecurring == options.IsRecurring.Value);
        if (!string.IsNullOrWhiteSpace(options.Merchant))
        {
            var merchant = options.Merchant.Trim().ToLower();
            query = query.Where(e => e.Merchant != null && e.Merchant.ToLower().Contains(merchant));
        }
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim().ToLower();
            query = query.Where(e =>
                (e.Description != null && e.Description.ToLower().Contains(search))
                || (e.Notes != null && e.Notes.ToLower().Contains(search))
                || (e.Merchant != null && e.Merchant.ToLower().Contains(search))
                || e.ExpenseTags.Any(et => et.Tag.DeletedAt == null
                    && et.Tag.Name.ToLower().Contains(search)));
        }

        var tagIds = options.TagIds.Distinct().ToArray();
        if (options.Untagged)
            query = query.Where(e => !e.ExpenseTags.Any(et => et.Tag.DeletedAt == null));
        else if (tagIds.Length > 0 && options.MatchAllTags)
            query = query.Where(e => e.ExpenseTags.Count(et =>
                tagIds.Contains(et.TagId) && et.Tag.DeletedAt == null) == tagIds.Length);
        else if (tagIds.Length > 0)
            query = query.Where(e => e.ExpenseTags.Any(et =>
                tagIds.Contains(et.TagId) && et.Tag.DeletedAt == null));

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = (options.SortBy.ToLowerInvariant(), options.SortDescending) switch
        {
            ("amount", false) => query.OrderBy(e => e.Amount),
            ("amount", true) => query.OrderByDescending(e => e.Amount),
            ("merchant", false) => query.OrderBy(e => e.Merchant),
            ("merchant", true) => query.OrderByDescending(e => e.Merchant),
            ("description", false) => query.OrderBy(e => e.Description),
            ("description", true) => query.OrderByDescending(e => e.Description),
            ("date", false) => query.OrderBy(e => e.Date),
            _ => query.OrderByDescending(e => e.Date)
        };

        var items = await ordered
            .ThenByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Include(e => e.Category)
            .Include(e => e.Account)
            .Include(e => e.CreditCard)
            .Include(e => e.Reimbursements.Where(r => r.DeletedAt == null))
            .Include(e => e.ExpenseTags)
                .ThenInclude(et => et.Tag)
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