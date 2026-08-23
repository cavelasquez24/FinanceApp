using System.Security.Claims;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/savings-replenishments")]
[Authorize]
public class SavingsReplenishmentsController : ControllerBase
{
    private readonly ISavingsReplenishmentService _service;

    public SavingsReplenishmentsController(ISavingsReplenishmentService service) => _service = service;

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetByUserIdAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SavingsReplenishmentDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(GetUserId(), id, cancellationToken);
        return Ok(ApiResponse<SavingsReplenishmentDto>.Ok(result));
    }

    [HttpGet("goal/{goalId:guid}")]
    public async Task<IActionResult> GetByGoal(Guid goalId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByGoalIdAsync(GetUserId(), goalId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SavingsReplenishmentDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SavingsReplenishmentCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<SavingsReplenishmentDto>.Ok(result, "Plan de reposición creado exitosamente"));
    }

    [HttpPost("{id:guid}/manual-debit")]
    public async Task<IActionResult> ManualDebit(Guid id, [FromBody] SavingsReplenishmentManualDebitDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.ApplyManualDebitAsync(GetUserId(), id, dto, cancellationToken);
        return Ok(ApiResponse<SavingsReplenishmentDto>.Ok(result, "Abono registrado exitosamente"));
    }

    [HttpPatch("{id:guid}/pause")]
    public async Task<IActionResult> Pause(Guid id, [FromBody] SavingsReplenishmentPauseDto? dto, CancellationToken cancellationToken)
    {
        var result = await _service.PauseAsync(GetUserId(), id, dto ?? new SavingsReplenishmentPauseDto(), cancellationToken);
        return Ok(ApiResponse<SavingsReplenishmentDto>.Ok(result, "Plan pausado"));
    }

    [HttpPatch("{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ResumeAsync(GetUserId(), id, cancellationToken);
        return Ok(ApiResponse<SavingsReplenishmentDto>.Ok(result, "Plan reanudado"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _service.CancelAsync(GetUserId(), id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Plan de reposición cancelado"));
    }

    [HttpPost("execute-cycle")]
    public async Task<IActionResult> ExecuteCycle(CancellationToken cancellationToken)
    {
        var result = await _service.ExecuteCycleDebitsAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<ReplenishmentCycleResultDto>.Ok(result));
    }
}
