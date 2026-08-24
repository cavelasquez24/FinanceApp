using System.Security.Claims;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Transfer;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/transfers")]
[Authorize]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;

    public TransfersController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] AccountTransferCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.CreateAsync(GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<AccountTransferCreateResultDto>.Ok(
            result, "Transferencia registrada exitosamente"));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _transferService.GetByUserIdAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccountTransferSummaryDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _transferService.GetByIdAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<AccountTransferDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _transferService.CancelAsync(id, GetUserId(), cancellationToken);
        return NoContent();
    }
}
