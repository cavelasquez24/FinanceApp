using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IEmergencyFundRestorationRepository : IBaseRepository<EmergencyFundRestoration>
{
    Task<EmergencyFundRestoration?> GetOwnedByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmergencyFundRestoration>> GetByGoalAsync(Guid goalId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetDueIdsAsync(
        Guid userId, DateOnly asOfDate, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalOutstandingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<decimal> GetRestoredByDateRangeAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compromiso de restauración imputable al ciclo: por cada restauración
    /// abierta cuya cuota siguiente vence en o antes de <paramref name="cycleEnd"/>,
    /// el menor entre la cuota programada y el pendiente. Incluye cuotas ya
    /// vencidas — siguen siendo un compromiso vivo del usuario.
    /// </summary>
    Task<decimal> GetScheduledCommitmentByCycleAsync(
        Guid userId, DateOnly cycleEnd, CancellationToken cancellationToken = default);
}
