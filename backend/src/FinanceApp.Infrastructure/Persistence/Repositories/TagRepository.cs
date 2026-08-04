using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class TagRepository : BaseRepository<Tag>, ITagRepository
{
    public TagRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Tag>> GetByUserAsync(Guid userId, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Tags
            .Include(t => t.ExpenseTags)
                .ThenInclude(et => et.Expense)
            .Where(t => t.UserId == userId && t.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(t => t.LastUsedAt)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetActiveByIdsAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        await _context.Tags
            .Where(t => t.UserId == userId && t.DeletedAt == null && ids.Contains(t.Id))
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Tags.CountAsync(t => t.UserId == userId && t.DeletedAt == null, cancellationToken);

    public Task<bool> ExistsActiveAsync(Guid userId, string normalizedName, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        _context.Tags.AnyAsync(t => t.UserId == userId
            && t.NormalizedName == normalizedName
            && t.DeletedAt == null
            && (!excludingId.HasValue || t.Id != excludingId.Value), cancellationToken);

    public async Task MergeAsync(Tag source, Tag target, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var sourceLinks = await _context.ExpenseTags
            .Where(et => et.TagId == source.Id)
            .ToListAsync(cancellationToken);
        var targetExpenseIds = await _context.ExpenseTags
            .Where(et => et.TagId == target.Id)
            .Select(et => et.ExpenseId)
            .ToListAsync(cancellationToken);
        var existing = targetExpenseIds.ToHashSet();

        foreach (var link in sourceLinks)
            if (!existing.Contains(link.ExpenseId))
                _context.ExpenseTags.Add(new ExpenseTag { ExpenseId = link.ExpenseId, TagId = target.Id });

        _context.ExpenseTags.RemoveRange(sourceLinks);
        source.MergedIntoTagId = target.Id;
        source.DeletedAt = DateTimeOffset.UtcNow;
        source.UpdatedAt = DateTimeOffset.UtcNow;
        target.LastUsedAt = DateTimeOffset.UtcNow;
        target.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TagExpenseMetric>> GetExpenseReportAsync(
        Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var metrics = await _context.ExpenseTags
            .Where(et => et.Tag.UserId == userId
                && et.Tag.DeletedAt == null
                && et.Expense.UserId == userId
                && et.Expense.DeletedAt == null
                && et.Expense.Date >= startDate
                && et.Expense.Date <= endDate)
            .GroupBy(et => new { et.TagId, et.Tag.Name, et.Tag.Color })
            .Select(group => new
            {
                group.Key.TagId,
                group.Key.Name,
                group.Key.Color,
                TotalAmount = group.Sum(et => et.Expense.Amount),
                ExpenseCount = group.Count()
            })
            .OrderByDescending(metric => metric.TotalAmount)
            .ToListAsync(cancellationToken);

        return metrics
            .Select(metric => new TagExpenseMetric(
                metric.TagId, metric.Name, metric.Color,
                metric.TotalAmount, metric.ExpenseCount))
            .ToList();
    }

    public async Task<(int TotalExpenses, int TaggedExpenses)> GetCoverageAsync(
        Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var expenses = _context.Expenses.Where(e => e.UserId == userId
            && e.DeletedAt == null && e.Date >= startDate && e.Date <= endDate);
        return (
            await expenses.CountAsync(cancellationToken),
            await expenses.CountAsync(e => e.ExpenseTags.Any(et => et.Tag.DeletedAt == null), cancellationToken));
    }
}
