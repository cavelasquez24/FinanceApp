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

    // ---- v2.0.1 — historial de movimientos ----

    /// <summary>
    /// Trae una SavingsGoal con sus colecciones de Contributions y
    /// Withdrawals cargadas (Include), validando ownership por UserId.
    /// Usar cuando el caller necesita ver/mostrar el historial completo.
    /// </summary>
    Task<SavingsGoal?> GetByIdWithHistoryAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddContributionAsync(
        SavingsGoalContribution contribution,
        CancellationToken cancellationToken = default);

    Task AddWithdrawalAsync(
        SavingsGoalWithdrawal withdrawal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suma de SavingsGoalContribution en un rango de fechas.
    /// Fuente de datos para el futuro Cash Flow Statement (sección 5).
    /// </summary>
    Task<decimal> GetTotalContributionsByDateRangeAsync(
        Guid userId, DateOnly start, DateOnly end,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalCashFlowWithdrawalsByDateRangeAsync(
        Guid userId, DateOnly start, DateOnly end,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suma de SavingsGoalWithdrawal cuyo Reason = Consumed (las únicas
    /// que representan consumo real) en un rango de fechas.
    /// </summary>
    Task<decimal> GetTotalConsumedWithdrawalsByDateRangeAsync(
        Guid userId, DateOnly start, DateOnly end,
        CancellationToken cancellationToken = default);
}
