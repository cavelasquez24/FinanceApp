using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IBudgetRepository : IBaseRepository<BudgetPeriod>
{
    /// <summary>
    /// Busca el presupuesto de un usuario para un mes y año específico.
    /// Incluye las categorías del presupuesto (BudgetCategories).
    /// </summary>
    Task<BudgetPeriod?> GetByUserAndPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Historial de presupuestos del usuario ordenados por fecha.
    /// </summary>
    Task<IEnumerable<BudgetPeriod>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}