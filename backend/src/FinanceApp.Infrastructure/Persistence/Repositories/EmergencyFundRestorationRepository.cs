using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class EmergencyFundRestorationRepository
    : BaseRepository<EmergencyFundRestoration>, IEmergencyFundRestorationRepository
{
    public EmergencyFundRestorationRepository(AppDbContext context) : base(context) { }

    public Task<EmergencyFundRestoration?> GetOwnedByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        _context.EmergencyFundRestorations
            .Include(r => r.SavingsGoal)
            .Include(r => r.Contributions.Where(c => c.DeletedAt == null))
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && r.DeletedAt == null, cancellationToken);

    public async Task<IReadOnlyList<EmergencyFundRestoration>> GetByGoalAsync(
        Guid goalId, Guid userId, CancellationToken cancellationToken = default) =>
        await _context.EmergencyFundRestorations
            .Where(r => r.SavingsGoalId == goalId && r.UserId == userId && r.DeletedAt == null)
            .OrderBy(r => r.Status == EmergencyFundRestorationStatus.Open ? 0 : 1)
            .ThenBy(r => r.TargetRestorationDate)
            .ThenBy(r => r.AcquisitionDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetDueIdsAsync(
        Guid userId, DateOnly asOfDate, CancellationToken cancellationToken = default) =>
        await _context.EmergencyFundRestorations
            .Where(r => r.UserId == userId
                && r.Status == EmergencyFundRestorationStatus.Open
                && r.NextScheduledDate <= asOfDate
                && r.DeletedAt == null)
            .OrderBy(r => r.NextScheduledDate)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

    public Task<decimal> GetTotalOutstandingAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        _context.EmergencyFundRestorations
            .Where(r => r.UserId == userId
                && r.Status == EmergencyFundRestorationStatus.Open
                && r.DeletedAt == null)
            .SumAsync(r => r.OriginalAmount - r.RestoredAmount, cancellationToken);

    public Task<decimal> GetRestoredByDateRangeAsync(
        Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
        _context.SavingsGoalContributions
            .Where(c => c.SavingsGoal.UserId == userId
                && c.EmergencyFundRestorationId != null
                && c.ContributionDate >= start && c.ContributionDate <= end
                && c.DeletedAt == null)
            .SumAsync(c => c.Amount, cancellationToken);
}
