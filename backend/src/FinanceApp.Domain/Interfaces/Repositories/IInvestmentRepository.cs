using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IInvestmentRepository : IBaseRepository<Investment>
{
    Task<IEnumerable<Investment>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valor total actual de todas las inversiones activas del usuario.
    /// Para el Dashboard y cálculo de patrimonio.
    /// </summary>
    Task<decimal> GetTotalCurrentValueAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    // ---- v2.0.1 — historial de aportes de caja ----

    Task AddContributionAsync(
        InvestmentContribution contribution,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suma de InvestmentContribution en un rango de fechas.
    /// Fuente de datos para el Cash Flow Statement (sección 5).
    /// </summary>
    Task<decimal> GetTotalContributionsByDateRangeAsync(
        Guid userId, DateOnly start, DateOnly end,
        CancellationToken cancellationToken = default);
}
