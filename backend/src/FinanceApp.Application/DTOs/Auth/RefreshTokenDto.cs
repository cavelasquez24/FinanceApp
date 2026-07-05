namespace FinanceApp.Application.DTOs.Auth;

/// <summary>
/// Datos que llegan cuando se solicita renovar el access token.
/// </summary>
public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}