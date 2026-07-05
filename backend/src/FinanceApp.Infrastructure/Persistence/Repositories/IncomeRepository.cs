using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class IncomeRepository : BaseRepository<Income>, IIncomeRepository
{
    public IncomeRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<Income> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? categoryId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // Construimos la consulta base
        var query = _context.Incomes
            .Include(i => i.Category)  // carga la categoría en la misma consulta
            .Where(i => i.UserId == userId && i.DeletedAt == null);

        // Aplicamos filtros opcionales
        if (categoryId.HasValue)
            query = query.Where(i => i.CategoryId == categoryId.Value);

        if (startDate.HasValue)
            query = query.Where(i => i.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(i => i.Date <= endDate.Value);

        // Contamos el total ANTES de paginar
        // para saber cuántas páginas hay en total
        var totalCount = await query.CountAsync(cancellationToken);

        // Aplicamos ordenamiento y paginación
        var items = await query
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)  // saltamos los registros de páginas anteriores
            .Take(pageSize)               // tomamos solo los de esta página
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<decimal> GetTotalByUserAndPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.Incomes
            .Where(i => i.UserId == userId
                     && i.Date.Month == month
                     && i.Date.Year == year
                     && i.DeletedAt == null)
            .SumAsync(i => i.Amount, cancellationToken);
    }
}