using System.Security.Claims;
using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/accounts/{accountId:guid}/reconciliations")]
[Authorize]
public class AccountReconciliationsController : ControllerBase
{
    private readonly IAccountReconciliationService _service;

    public AccountReconciliationsController(IAccountReconciliationService service) =>
        _service = service;

    private Guid GetUserId() => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value!);

    [HttpGet("preview")]
    public async Task<IActionResult> GetPreview(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await _service.GetPreviewAsync(accountId, GetUserId(), cancellationToken);
        return Ok(ApiResponse<ReconciliationPreviewDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Apply(
        Guid accountId,
        [FromBody] ReconciliationCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.ApplyAsync(accountId, GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<ReconciliationResponseDto>.Ok(
            result, "Conciliación aplicada correctamente"));
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory(
        Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetHistoryAsync(
            accountId, GetUserId(), page, pageSize, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReconciliationResponseDto>>.Ok(result));
    }
}
