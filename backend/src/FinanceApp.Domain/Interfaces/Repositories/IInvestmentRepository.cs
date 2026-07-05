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
}