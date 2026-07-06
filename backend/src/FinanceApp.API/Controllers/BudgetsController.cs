using FinanceApp.Application.DTOs.Budget;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/budgets")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _budgetService.GetAllAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<IEnumerable<BudgetResponseDto>>.Ok(result));
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var result = await _budgetService.GetCurrentAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<BudgetResponseDto?>.Ok(result));
    }

    [HttpGet("{year:int}/{month:int}")]
    public async Task<IActionResult> GetByPeriod(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var result = await _budgetService.GetByPeriodAsync(
            GetUserId(), month, year, cancellationToken);
        return Ok(ApiResponse<BudgetResponseDto?>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] BudgetCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _budgetService.CreateAsync(
            GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<BudgetResponseDto>.Ok(
            result, "Presupuesto creado exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] BudgetUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _budgetService.UpdateAsync(
            id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<BudgetResponseDto>.Ok(
            result, "Presupuesto actualizado exitosamente"));
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _budgetService.GetStatusAsync(
            id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<BudgetStatusDto>.Ok(result));
    }
}