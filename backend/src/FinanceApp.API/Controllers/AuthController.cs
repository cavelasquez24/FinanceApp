using FinanceApp.Application.DTOs.Auth;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

/// <summary>
/// Controlador de autenticación.
/// Maneja registro, login, refresh token y logout.
/// 
/// El controller es delgado: solo recibe, delega y responde.
/// Toda la lógica está en AuthService.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(dto, cancellationToken);
        return StatusCode(201, ApiResponse<AuthResponseDto>.Ok(result, "Registro exitoso"));
    }

    /// <summary>
    /// Inicia sesión y retorna los tokens de autenticación.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result));
    }

    /// <summary>
    /// Renueva el access token usando el refresh token.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(
            dto.RefreshToken, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result));
    }

    /// <summary>
    /// Cierra sesión revocando todos los refresh tokens del usuario.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // Extrae el UserId del JWT que viene en el header Authorization
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

        await _authService.LogoutAsync(userId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Sesión cerrada exitosamente"));
    }
}