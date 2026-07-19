using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Investment;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/investments")]
[Authorize]
public class InvestmentsController : ControllerBase
{
    private readonly IInvestmentService _investmentService;

    public InvestmentsController(IInvestmentService investmentService)
    {
        _investmentService = investmentService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _investmentService.GetAllAsync(
            GetUserId(), cancellationToken);
        return Ok(ApiResponse<IEnumerable<InvestmentResponseDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _investmentService.GetByIdAsync(
            id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<InvestmentResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] InvestmentCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _investmentService.CreateAsync(
            GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<InvestmentResponseDto>.Ok(
            result, "Inversión registrada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] InvestmentUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _investmentService.UpdateAsync(
            id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<InvestmentResponseDto>.Ok(
            result, "Inversión actualizada exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, CancellationToken cancellationToken)
    {
        await _investmentService.DeleteAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Inversión eliminada exitosamente"));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _investmentService.GetSummaryAsync(
            GetUserId(), cancellationToken);
        return Ok(ApiResponse<InvestmentSummaryDto>.Ok(result));
    }

    [HttpGet("{id:guid}/records")]
    public async Task<IActionResult> GetRecords(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _investmentService.GetRecordsAsync(
            id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<IEnumerable<InvestmentRecordResponseDto>>.Ok(result));
    }

    [HttpPost("{id:guid}/records")]
    public async Task<IActionResult> AddRecord(
        Guid id,
        [FromBody] InvestmentRecordCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _investmentService.AddRecordAsync(
            id, GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<InvestmentRecordResponseDto>.Ok(
            result, "Registro agregado exitosamente"));
    }

    // v2.0.1 — nuevo endpoint. Aporte de caja a la inversión (aumenta
    // costo base). Distinto de /records, que es revalorización de mercado.
    [HttpPost("{id:guid}/contributions")]
    public async Task<IActionResult> AddContribution(
        Guid id,
        [FromBody] InvestmentContributionCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _investmentService.AddContributionAsync(
            id, GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<InvestmentContributionResponseDto>.Ok(
            result, "Aporte registrado exitosamente"));
    }
}
