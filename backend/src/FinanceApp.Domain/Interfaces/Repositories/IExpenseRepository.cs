using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IExpenseRepository : IBaseRepository<Expense>
{
    Task<(IEnumerable<Expense> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? categoryId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalByUserAndPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gastos agrupados por categoría para un período.
    /// Para el gráfico de torta del Dashboard.
    /// La tupla retorna: CategoryId, Nombre, Color, Total gastado.
    /// </summary>
    Task<IEnumerable<(Guid CategoryId, string CategoryName, string CategoryColor, decimal Total)>>
        GetByCategoryAsync(
            Guid userId,
            int month,
            int year,
            CancellationToken cancellationToken = default);

    Task<decimal> GetTotalByDateRangeAsync(
    Guid userId, DateOnly startDate, DateOnly endDate,
    CancellationToken cancellationToken = default);

    Task<IEnumerable<(Guid CategoryId, string CategoryName, string CategoryColor, decimal Total)>>
    GetByCategoryByDateRangeAsync(
        Guid userId, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default);
}