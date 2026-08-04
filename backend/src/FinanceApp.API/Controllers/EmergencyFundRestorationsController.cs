using System.Security.Claims;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class EmergencyFundRestorationsController : ControllerBase
{
    private readonly IEmergencyFundRestorationService _service;

    public EmergencyFundRestorationsController(IEmergencyFundRestorationService service) => _service = service;

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value!);

    [HttpGet("savings-goals/{goalId:guid}/restorations")]
    public async Task<IActionResult> GetByGoal(Guid goalId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByGoalAsync(goalId, GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<EmergencyFundRestorationResponseDto>>.Ok(result));
    }

    [HttpPost("savings-goals/{goalId:guid}/emergency-fund-uses")]
    public async Task<IActionResult> CreateUse(
        Guid goalId, [FromBody] EmergencyFundUseCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateUseAsync(goalId, GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<EmergencyFundRestorationResponseDto>.Ok(
            result, "Uso y restauración del fondo registrados"));
    }

    [HttpPost("emergency-fund-restorations/{id:guid}/payments")]
    public async Task<IActionResult> RegisterPayment(
        Guid id, [FromBody] EmergencyFundRestorationPaymentDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.RegisterPaymentAsync(id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<EmergencyFundRestorationResponseDto>.Ok(result, "Aporte de restauración registrado"));
    }

    [HttpPost("emergency-fund-restorations/process-due")]
    public async Task<IActionResult> ProcessDue(
        [FromQuery] DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        var processingDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _service.ProcessDueAsync(GetUserId(), processingDate, cancellationToken);
        return Ok(ApiResponse<DueRestorationProcessingResultDto>.Ok(
            result, result.ProcessedCount > 0
                ? "Aportes programados aplicados"
                : "No hay aportes programados por aplicar"));
    }

    [HttpPost("emergency-fund-restorations/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.CancelAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<EmergencyFundRestorationResponseDto>.Ok(result, "Restauración cancelada"));
    }
}
