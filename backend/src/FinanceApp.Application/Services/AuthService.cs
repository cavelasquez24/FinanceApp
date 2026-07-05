using System.Security.Cryptography;
using System.Text;
using FinanceApp.Application.DTOs.Auth;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Domain.Interfaces.Services;
using FinanceApp.Application.Interfaces;
namespace FinanceApp.Application.Services;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Servicio de autenticación.
/// Contiene toda la lógica de negocio relacionada con
/// registro, login, refresh token y logout.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Verificar que el email no esté registrado
        var emailExists = await _userRepository.ExistsByEmailAsync(
            dto.Email, cancellationToken);

        if (emailExists)
            throw new DomainException(
                "EMAIL_ALREADY_EXISTS",
                "Este email ya está registrado");

        // 2. Crear el usuario con la contraseña hasheada
        var user = new User
        {
            Email = dto.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            CurrencyCode = "USD"
        };

        await _userRepository.CreateAsync(user, cancellationToken);

        // 3. Generar tokens
        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Buscar el usuario por email
        var user = await _userRepository.GetByEmailAsync(
            dto.Email, cancellationToken);

        // 2. Verificar credenciales
        // Usamos el mismo mensaje para email incorrecto y contraseña incorrecta
        // para no revelar si el email existe o no
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedException(
                "INVALID_CREDENTIALS",
                "Email o contraseña incorrectos");

        // 3. Generar tokens
        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        // 1. Hashear el token recibido para buscar en BD
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        // 2. Buscar el token en BD
        var storedToken = await _refreshTokenRepository
            .GetByTokenHashAsync(tokenHash, cancellationToken);

        // 3. Validar que el token existe, no está revocado y no expiró
        if (storedToken == null || !storedToken.IsActive)
            throw new UnauthorizedException(
                "INVALID_REFRESH_TOKEN",
                "El refresh token es inválido o ha expirado");

        // 4. Revocar el token actual (rotación de tokens)
        await _refreshTokenRepository.RevokeAsync(storedToken, cancellationToken);

        // 5. Generar nuevos tokens
        return await GenerateAuthResponseAsync(storedToken.User, cancellationToken);
    }

    public async Task LogoutAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        // Revocar todos los refresh tokens del usuario
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Método privado que genera el access token y refresh token
    /// y los guarda en BD. Reutilizado por Register, Login y Refresh.
    /// </summary>
    private async Task<AuthResponseDto> GenerateAuthResponseAsync(
        User user, CancellationToken cancellationToken)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var (refreshToken, refreshTokenHash) = _jwtService.GenerateRefreshToken();

        var refreshTokenExpirationDays = int.Parse(
            _configuration["JwtSettings:RefreshTokenExpirationDays"]!);

        // Guardar el refresh token hasheado en BD
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshTokenExpirationDays)
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            User = new UserInfoDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                CurrencyCode = user.CurrencyCode
            }
        };
    }
}