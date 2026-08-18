using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class CreditCardRepository : BaseRepository<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<CreditCard>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _context.CreditCards
            .Where(c => c.UserId == userId && c.DeletedAt == null)
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<CreditCard?> GetByIdWithHistoryAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        _context.CreditCards
            .Include(c => c.Transactions.Where(t => t.DeletedAt == null))
            .Include(c => c.Payments.Where(p => p.DeletedAt == null))
                .ThenInclude(p => p.SourceAccount)
            .FirstOrDefaultAsync(
                c => c.Id == id && c.UserId == userId && c.DeletedAt == null,
                cancellationToken);

    public Task<CreditCardTransaction?> GetTransactionBySourceAsync(
        Guid userId, string sourceType, Guid sourceId,
        CancellationToken cancellationToken = default) =>
        _context.CreditCardTransactions.FirstOrDefaultAsync(
            t => t.UserId == userId
                && t.SourceType == sourceType
                && t.SourceId == sourceId
                && t.DeletedAt == null,
            cancellationToken);

    public Task<CreditCardPayment?> GetPaymentByIdempotencyKeyAsync(
        Guid userId, Guid creditCardId, Guid idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _context.CreditCardPayments
            .Include(p => p.SourceAccount)
            .Include(p => p.CreditCard)
            .FirstOrDefaultAsync(
                p => p.UserId == userId
                    && p.CreditCardId == creditCardId
                    && p.IdempotencyKey == idempotencyKey
                    && p.DeletedAt == null,
                cancellationToken);
    public Task<CreditCardPayment?> GetPaymentByIdAsync(
        Guid paymentId, Guid creditCardId, Guid userId,
        CancellationToken cancellationToken = default) =>
        _context.CreditCardPayments
            .Include(p => p.SourceAccount)
            .Include(p => p.CreditCard)
            .Include(p => p.CommissionExpense)
            .FirstOrDefaultAsync(
                p => p.Id == paymentId
                    && p.CreditCardId == creditCardId
                    && p.UserId == userId
                    && p.DeletedAt == null,
                cancellationToken);

    public Task<decimal> GetTotalCurrentBalanceAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        _context.CreditCards
            .Where(c => c.UserId == userId && c.DeletedAt == null)
            .SumAsync(c => c.CurrentBalance, cancellationToken);
    public Task<decimal> GetTotalPrincipalPaidByDateRangeAsync(
        Guid userId, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        _context.CreditCardPayments
            .Where(payment => payment.UserId == userId && payment.DeletedAt == null
                && payment.VoidedAt == null && payment.PaymentDate >= startDate
                && payment.PaymentDate <= endDate)
            .SumAsync(payment => payment.PrincipalAmount, cancellationToken);

    public async Task SaveTransactionAsync(
        CreditCardTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(transaction).State == EntityState.Detached)
            await _context.CreditCardTransactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SavePaymentAsync(
        CreditCardPayment payment,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(payment).State == EntityState.Detached)
            await _context.CreditCardPayments.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
