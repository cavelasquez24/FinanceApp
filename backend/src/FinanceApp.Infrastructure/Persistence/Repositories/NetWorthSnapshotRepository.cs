using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class NetWorthSnapshotRepository : INetWorthSnapshotRepository
{
    private readonly AppDbContext _context;

    public NetWorthSnapshotRepository(AppDbContext context) => _context = context;

    public async Task<NetWorthSnapshot?> GetByDateAsync(
        Guid userId, DateOnly date, CancellationToken ct = default) =>
        await _context.NetWorthSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SnapshotDate == date && s.DeletedAt == null, ct);

    public async Task<IReadOnlyList<NetWorthSnapshot>> GetRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await _context.NetWorthSnapshots
            .Where(s => s.UserId == userId && s.SnapshotDate >= from && s.SnapshotDate <= to && s.DeletedAt == null)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(ct);

    public async Task UpsertAsync(NetWorthSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await GetByDateAsync(snapshot.UserId, snapshot.SnapshotDate, ct);
        if (existing is null)
        {
            await _context.NetWorthSnapshots.AddAsync(snapshot, ct);
        }
        else
        {
            existing.TotalAssets = snapshot.TotalAssets;
            existing.TotalLiabilities = snapshot.TotalLiabilities;
            existing.NetWorth = snapshot.NetWorth;
            existing.CashAccounts = snapshot.CashAccounts;
            existing.SavingsAccounts = snapshot.SavingsAccounts;
            existing.InvestmentPositions = snapshot.InvestmentPositions;
            existing.DebtLiabilities = snapshot.DebtLiabilities;
            existing.CreditCardLiabilities = snapshot.CreditCardLiabilities;
            existing.Source = snapshot.Source;
        }
        await _context.SaveChangesAsync(ct);
    }
}
