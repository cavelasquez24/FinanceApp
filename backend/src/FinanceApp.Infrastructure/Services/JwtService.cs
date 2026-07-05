using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Implementación del servicio JWT.
/// Genera y valida tokens de autenticación.
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = configuration["JwtSettings:SecretKey"]!;
        _issuer = configuration["JwtSettings:Issuer"]!;
        _audience = configuration["JwtSettings:Audience"]!;
        _accessTokenExpirationMinutes = int.Parse(
            configuration["JwtSettings:AccessTokenExpirationMinutes"]!);
    }

    /// <summary>
    /// Genera un JWT firmado con los datos del usuario.
    /// El token contiene claims (datos) que el servidor puede leer
    /// sin consultar la base de datos.
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        // Claims: datos que van dentro del token
        var claims = new[]
        {
            // Sub (subject): identificador del usuario
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            // Email del usuario
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            // Nombre completo
            new Claim(ClaimTypes.Name, user.FullName),
            // ID único del token (evita reutilización)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Clave de firma — debe tener al menos 32 caracteres
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Genera un refresh token aleatorio seguro.
    /// Retorna el token en texto plano (para el cliente)
    /// y su hash SHA-256 (para almacenar en BD).
    /// </summary>
    public (string Token, string TokenHash) GenerateRefreshToken()
    {
        // Genera 64 bytes aleatorios criptográficamente seguros
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);

        // Hash SHA-256 del token para almacenar en BD
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        return (token, tokenHash);
    }

    /// <summary>
    /// Extrae el UserId de un token expirado.
    /// Útil en el endpoint refresh-token.
    /// </summary>
    public Guid? GetUserIdFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_secretKey)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            // NO valida la expiración — el token puede estar vencido
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(
                token, tokenValidationParameters, out _);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }
}