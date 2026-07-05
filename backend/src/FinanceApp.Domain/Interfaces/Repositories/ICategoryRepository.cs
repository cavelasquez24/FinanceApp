using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface ICategoryRepository : IBaseRepository<Category>
{
    /// <summary>
    /// Retorna las categorías del sistema (user_id = NULL)
    /// más las categorías propias del usuario.
    /// </summary>
    Task<IEnumerable<Category>> GetByUserIdAsync(
        Guid userId,
        CategoryType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si el usuario ya tiene una categoría
    /// con ese nombre y tipo para evitar duplicados.
    /// </summary>
    Task<bool> ExistsByNameAsync(
        Guid userId,
        string name,
        CategoryType type,
        CancellationToken cancellationToken = default);
}