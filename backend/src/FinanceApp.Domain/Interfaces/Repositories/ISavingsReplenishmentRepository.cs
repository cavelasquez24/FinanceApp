using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface ISavingsReplenishmentRepository : IBaseRepository<SavingsReplenishment>
{
    /// <summary>
    /// Trae una SavingsReplenishment con SavingsGoal y SourceAccount
    /// cargados, validando ownership por UserId.
    /// </summary>
    Task<SavingsReplenishment?> GetOwnedByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavingsReplenishment>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavingsReplenishment>> GetByGoalIdAsync(
        Guid goalId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Planes elegibles para el débito automático del ciclo:
    /// Status = Active, AutoDebitEnabled = true, IsPaused = false.
    /// </summary>
    Task<IReadOnlyList<SavingsReplenishment>> GetActiveForAutoDebitAsync(
        Guid userId, CancellationToken cancellationToken = default);
}
