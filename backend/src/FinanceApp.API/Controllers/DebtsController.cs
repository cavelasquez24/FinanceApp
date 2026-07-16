using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Debt;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/debts")]
[Authorize]
public class DebtsController : ControllerBase
{
    private readonly IDebtService _debtService;

    public DebtsController(IDebtService debtService)
    {
        _debtService = debtService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _debtService.GetAllAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<IEnumerable<DebtResponseDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _debtService.GetByIdAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<DebtResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] DebtCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _debtService.CreateAsync(GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<DebtResponseDto>.Ok(
            result, "Deuda registrada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] DebtUpdateDto dto, CancellationToken cancellationToken)
    {
        var result = await _debtService.UpdateAsync(id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<DebtResponseDto>.Ok(
            result, "Deuda actualizada exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _debtService.DeleteAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Deuda eliminada exitosamente"));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _debtService.GetSummaryAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<DebtSummaryDto>.Ok(result));
    }

    [HttpGet("{id:guid}/payments")]
    public async Task<IActionResult> GetPayments(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _debtService.GetPaymentsAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<IEnumerable<DebtPaymentResponseDto>>.Ok(result));
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> AddPayment(
        Guid id, [FromBody] DebtPaymentCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _debtService.AddPaymentAsync(id, GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<DebtPaymentResponseDto>.Ok(
            result, "Pago registrado exitosamente"));
    }
}