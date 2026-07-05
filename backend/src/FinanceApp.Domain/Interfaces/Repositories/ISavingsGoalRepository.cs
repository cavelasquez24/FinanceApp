using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface ISavingsGoalRepository : IBaseRepository<SavingsGoal>
{
    Task<IEnumerable<SavingsGoal>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suma del dinero ahorrado en todas las metas activas.
    /// Para el Dashboard y cálculo de patrimonio.
    /// </summary>
    Task<decimal> GetTotalSavedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}