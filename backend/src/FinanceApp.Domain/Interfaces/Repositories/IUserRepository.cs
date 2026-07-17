using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    /// <summary>
    /// Busca un usuario por email para el proceso de login.
    /// Retorna null si no existe.
    /// </summary>
    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si ya existe un usuario con ese email.
    /// Para evitar duplicados en el registro.
    /// </summary>
    Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

}
