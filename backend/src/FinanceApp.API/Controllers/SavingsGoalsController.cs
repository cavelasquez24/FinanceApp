using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/savings-goals")]
[Authorize]
public class SavingsGoalsController : ControllerBase
{
    private readonly ISavingsGoalService _savingsGoalService;

    public SavingsGoalsController(ISavingsGoalService savingsGoalService)
    {
        _savingsGoalService = savingsGoalService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _savingsGoalService.GetAllAsync(
            GetUserId(), cancellationToken);
        return Ok(ApiResponse<IEnumerable<SavingsGoalResponseDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _savingsGoalService.GetByIdAsync(
            id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<SavingsGoalResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SavingsGoalCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _savingsGoalService.CreateAsync(
            GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<SavingsGoalResponseDto>.Ok(
            result, "Meta de ahorro creada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] SavingsGoalUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _savingsGoalService.UpdateAsync(
            id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<SavingsGoalResponseDto>.Ok(
            result, "Meta actualizada exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        await _savingsGoalService.DeleteAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Meta eliminada exitosamente"));
    }

    [HttpPatch("{id:guid}/deposit")]
    public async Task<IActionResult> Deposit(
        Guid id,
        [FromBody] DepositDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _savingsGoalService.DepositAsync(
            id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<SavingsGoalResponseDto>.Ok(
            result, "Aporte registrado exitosamente"));
    }
}