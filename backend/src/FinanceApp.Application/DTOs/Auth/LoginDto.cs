namespace FinanceApp.Application.DTOs.Auth;

/// <summary>
/// Datos que llegan cuando un usuario inicia sesión.
/// </summary>
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}