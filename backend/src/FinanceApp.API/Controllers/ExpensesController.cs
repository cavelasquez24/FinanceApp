using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Expense;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ExpenseFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetAllAsync(
            GetUserId(), filter, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ExpenseResponseDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetByIdAsync(
            id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<ExpenseResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ExpenseCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.CreateAsync(
            GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<ExpenseResponseDto>.Ok(
            result, "Gasto registrado exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ExpenseUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.UpdateAsync(
            id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<ExpenseResponseDto>.Ok(
            result, "Gasto actualizado exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _expenseService.DeleteAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Gasto eliminado exitosamente"));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetSummaryAsync(
            GetUserId(), month, year, cancellationToken);
        return Ok(ApiResponse<ExpenseSummaryDto>.Ok(result));
    }
}