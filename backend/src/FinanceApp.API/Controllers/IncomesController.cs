using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Income;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/incomes")]
[Authorize] // todos los endpoints requieren autenticación
public class IncomesController : ControllerBase
{
    private readonly IIncomeService _incomeService;

    public IncomesController(IIncomeService incomeService)
    {
        _incomeService = incomeService;
    }

    /// <summary>
    /// Obtiene el UserId del JWT del usuario autenticado.
    /// Evitamos repetir este código en cada método.
    /// </summary>
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] IncomeFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _incomeService.GetAllAsync(
            GetUserId(), filter, cancellationToken);
        return Ok(ApiResponse<PagedResponse<IncomeResponseDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _incomeService.GetByIdAsync(
            id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<IncomeResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] IncomeCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _incomeService.CreateAsync(
            GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<IncomeResponseDto>.Ok(
            result, "Ingreso registrado exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] IncomeUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _incomeService.UpdateAsync(
            id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<IncomeResponseDto>.Ok(
            result, "Ingreso actualizado exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _incomeService.DeleteAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Ingreso eliminado exitosamente"));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var result = await _incomeService.GetSummaryAsync(
            GetUserId(), month, year, cancellationToken);
        return Ok(ApiResponse<IncomeSummaryDto>.Ok(result));
    }
}