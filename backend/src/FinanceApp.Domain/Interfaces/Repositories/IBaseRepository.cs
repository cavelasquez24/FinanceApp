namespace FinanceApp.Domain.Interfaces.Repositories;

/// <summary>
/// Interfaz genérica con operaciones CRUD comunes.
/// Todas las interfaces de repositorio heredan de esta.
/// T debe ser una clase (entidad de dominio).
/// </summary>
public interface IBaseRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}