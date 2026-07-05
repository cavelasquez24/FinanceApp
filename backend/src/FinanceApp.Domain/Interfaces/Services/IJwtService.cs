using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Services;

public interface IJwtService
{
    /// <summary>
    /// Genera un Access Token JWT válido por 15 minutos.
    /// Contiene el UserId y email del usuario en el payload.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Genera un Refresh Token aleatorio seguro.
    /// Retorna:
    ///   Token     → texto plano para enviar al cliente
    ///   TokenHash → hash SHA-256 para almacenar en BD
    /// </summary>
    (string Token, string TokenHash) GenerateRefreshToken();

    /// <summary>
    /// Extrae el UserId de un access token expirado.
    /// Útil en el endpoint refresh-token donde el JWT ya venció
    /// Para saber a quién pertenece
    /// </summary>
    Guid? GetUserIdFromExpiredToken(string token);
}