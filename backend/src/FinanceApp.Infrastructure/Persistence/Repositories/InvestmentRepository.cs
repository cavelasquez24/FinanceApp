using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class InvestmentRepository : BaseRepository<Investment>, IInvestmentRepository
{
    public InvestmentRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Investment>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Investments
            .Include(i => i.Records)
            .Where(i => i.UserId == userId && i.DeletedAt == null)
            .OrderByDescending(i => i.PurchaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalCurrentValueAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Investments
            .Where(i => i.UserId == userId
                     && i.IsActive
                     && i.DeletedAt == null)
            .SumAsync(i => i.CurrentValue, cancellationToken);
    }

    // ---- v2.0.1 — historial de aportes de caja ----

    // Agregamos directo al DbSet de la entidad hija (no vía
    // investment.Contributions.Add + UpdateAsync(investment)) para evitar
    // el mismo riesgo de concurrencia visto en Debt/SavingsGoal con hijos
    // nuevos y Guid.NewGuid() en BaseEntity.Id.
    public async Task AddContributionAsync(
        InvestmentContribution contribution,
        CancellationToken cancellationToken = default)
    {
        await _context.InvestmentContributions.AddAsync(contribution, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalContributionsByDateRangeAsync(
        Guid userId, DateOnly start, DateOnly end,
        CancellationToken cancellationToken = default)
    {
        return await _context.InvestmentContributions
            .Where(c => c.Investment.UserId == userId
                && c.ContributionDate >= start && c.ContributionDate <= end
                && c.DeletedAt == null)
            .SumAsync(c => c.Amount, cancellationToken);
    }
}
