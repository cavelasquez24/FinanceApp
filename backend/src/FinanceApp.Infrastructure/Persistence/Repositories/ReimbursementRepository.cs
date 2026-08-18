using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class ReimbursementRepository : BaseRepository<Reimbursement>, IReimbursementRepository
{
    public ReimbursementRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Reimbursement>> GetByUserIdAsync(
        Guid userId, DateOnly? startDate, DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = Details(_context.Reimbursements)
            .Where(r => r.UserId == userId && r.DeletedAt == null);
        if (startDate.HasValue) query = query.Where(r => r.Date >= startDate.Value);
        if (endDate.HasValue) query = query.Where(r => r.Date <= endDate.Value);
        return await query.OrderByDescending(r => r.Date).ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Reimbursement?> GetByIdWithDetailsAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Details(_context.Reimbursements).FirstOrDefaultAsync(
            r => r.Id == id && r.UserId == userId && r.DeletedAt == null, cancellationToken);

    public Task<Reimbursement?> GetByIdempotencyKeyAsync(
        Guid userId, Guid idempotencyKey, CancellationToken cancellationToken = default) =>
        Details(_context.Reimbursements).FirstOrDefaultAsync(
            r => r.UserId == userId && r.IdempotencyKey == idempotencyKey && r.DeletedAt == null,
            cancellationToken);

    public Task<decimal> GetTotalByExpenseIdAsync(
        Guid userId, Guid expenseId, Guid? excludingId, CancellationToken cancellationToken = default) =>
        _context.Reimbursements.Where(r => r.UserId == userId && r.ExpenseId == expenseId
                && r.DeletedAt == null && (!excludingId.HasValue || r.Id != excludingId.Value))
            .SumAsync(r => (decimal?)r.Amount, cancellationToken).ContinueWith(t => t.Result ?? 0m, cancellationToken);

    public Task<decimal> GetTotalByDateRangeAsync(
        Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
        _context.Reimbursements.Where(r => r.UserId == userId && r.DeletedAt == null
                && r.Date >= startDate && r.Date <= endDate)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken).ContinueWith(t => t.Result ?? 0m, cancellationToken);

    private static IQueryable<Reimbursement> Details(IQueryable<Reimbursement> query) =>
        query.Include(r => r.Expense).Include(r => r.Account).Include(r => r.CreditCard);
}
