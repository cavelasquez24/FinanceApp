using System.Security.Claims;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Reimbursement;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/reimbursements")]
[Authorize]
public class ReimbursementsController : ControllerBase
{
    private readonly IReimbursementService _service;
    public ReimbursementsController(IReimbursementService service) => _service = service;
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<ReimbursementResponseDto>>.Ok(
            await _service.GetAllAsync(UserId, startDate, endDate, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ReimbursementResponseDto>.Ok(await _service.GetByIdAsync(id, UserId, cancellationToken)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReimbursementCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(UserId, dto, cancellationToken);
        return StatusCode(201, ApiResponse<ReimbursementResponseDto>.Ok(result, "Reembolso registrado exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ReimbursementUpdateDto dto, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ReimbursementResponseDto>.Ok(
            await _service.UpdateAsync(id, UserId, dto, cancellationToken), "Reembolso actualizado exitosamente"));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, UserId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Reembolso anulado exitosamente"));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ReimbursementSummaryDto>.Ok(
            await _service.GetSummaryAsync(UserId, startDate, endDate, cancellationToken)));
}
