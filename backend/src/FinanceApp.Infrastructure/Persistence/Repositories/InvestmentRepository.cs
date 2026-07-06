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
}