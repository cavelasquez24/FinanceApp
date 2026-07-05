using FinanceApp.Application.DTOs.Auth;

namespace FinanceApp.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}