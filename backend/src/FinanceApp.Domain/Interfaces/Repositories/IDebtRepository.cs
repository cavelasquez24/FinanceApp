using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IDebtRepository : IBaseRepository<Debt>
{
    Task<IEnumerable<Debt>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saldo pendiente total de todas las deudas activas del usuario.
    /// Para el Dashboard y cálculo de patrimonio neto.
    /// </summary>
    Task<decimal> GetTotalCurrentBalanceAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}