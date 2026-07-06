using FinanceApp.Application.DTOs.Auth;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetProfileAsync(
            GetUserId(), cancellationToken);
        return Ok(ApiResponse<UserInfoDto>.Ok(result));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] ProfileUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateProfileAsync(
            GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<UserInfoDto>.Ok(result, "Perfil actualizado exitosamente"));
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto,
        CancellationToken cancellationToken)
    {
        await _profileService.ChangePasswordAsync(
            GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Contraseña actualizada exitosamente"));
    }

    [HttpPatch("currency")]
    public async Task<IActionResult> ChangeCurrency(
        [FromBody] ChangeCurrencyDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _profileService.ChangeCurrencyAsync(
            GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<UserInfoDto>.Ok(
            result, "Moneda actualizada exitosamente"));
    }
}