using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class DebtRepository : BaseRepository<Debt>, IDebtRepository
{
    public DebtRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Debt>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.Payments)
            .Where(d => d.UserId == userId && d.DeletedAt == null)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalCurrentBalanceAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.UserId == userId && d.IsActive && d.DeletedAt == null)
            .SumAsync(d => d.CurrentBalance, cancellationToken);
    }

    // Mismo fix aplicado en BudgetRepository: al agregar un DebtPayment a
    // debt.Payments y luego llamar UpdateAsync(debt), _dbSet.Update(entity)
    // marca la entidad hija nueva como Modified en vez de Added (por el
    // Guid.NewGuid() en BaseEntity.Id), lanzando DbUpdateConcurrencyException.
    // Saltamos _dbSet.Update() y dejamos que el change tracker de EF Core
    // detecte los cambios por sí mismo.
    public override async Task<Debt> UpdateAsync(
        Debt entity, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<decimal> GetTotalPaymentsByDateRangeAsync(
    Guid userId, DateOnly start, DateOnly end,
    CancellationToken cancellationToken = default)
    {
        return await _context.DebtPayments
            .Where(p => p.Debt.UserId == userId
                && p.PaymentDate >= start && p.PaymentDate <= end
                && p.DeletedAt == null)
            .SumAsync(p => p.Amount, cancellationToken);
    }
}