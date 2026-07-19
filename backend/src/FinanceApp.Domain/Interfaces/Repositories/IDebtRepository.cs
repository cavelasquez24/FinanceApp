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

    /// <summary>
    /// Suma de pagos de deuda (Amount) dentro de un rango de fechas.
    /// Para el Dashboard: métrica de "cash out" por pagos de deuda,
    /// separada de Expenses.
    /// </summary>
    Task<decimal> GetTotalPaymentsByDateRangeAsync(
        Guid userId,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// v2.0.1 — Suma solo la porción de capital (PrincipalAmount) pagada
    /// en un rango de fechas. Para el Cash Flow Statement (sección 5):
    /// distinto de GetTotalPaymentsByDateRangeAsync, que suma el pago
    /// total (capital + interés).
    /// </summary>
    Task<decimal> GetTotalPrincipalPaidByDateRangeAsync(
        Guid userId,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default);
}