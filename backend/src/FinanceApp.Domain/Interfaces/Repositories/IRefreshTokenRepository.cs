using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// Busca un refresh token por su hash.
    /// Incluye el usuario asociado para no hacer una segunda consulta.
    /// </summary>
    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<RefreshToken> CreateAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoca un token específico (logout de un dispositivo).
    /// </summary>
    Task RevokeAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoca todos los tokens del usuario (logout de todos los dispositivos).
    /// </summary>
    Task RevokeAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}