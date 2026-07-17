using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IIncomeRepository : IBaseRepository<Income>
{
    /// <summary>
    /// Obtiene los ingresos de un usuario con filtros y paginación.
    /// Retorna los items y el total de registros para paginar.
    /// </summary>
    Task<(IEnumerable<Income> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? categoryId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Total de ingresos de un usuario en un mes y año específico.
    /// Usado por el Dashboard.
    /// </summary>
    Task<decimal> GetTotalByUserAndPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalByDateRangeAsync(
    Guid userId, DateOnly startDate, DateOnly endDate,
    CancellationToken cancellationToken = default);
}