using System.Security.Claims;
using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IFinancialAccountService _accountService;

    public AccountsController(IFinancialAccountService accountService)
    {
        _accountService = accountService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _accountService.GetAllAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FinancialAccountResponseDto>>.Ok(result));
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int count = 20, CancellationToken cancellationToken = default)
    {
        var result = await _accountService.GetRecentTransactionsAsync(
            GetUserId(), count, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccountTransactionResponseDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] FinancialAccountCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _accountService.CreateAsync(GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<FinancialAccountResponseDto>.Ok(
            result, "Cuenta creada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] FinancialAccountUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _accountService.UpdateAsync(
            id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<FinancialAccountResponseDto>.Ok(
            result, "Cuenta actualizada exitosamente"));
    }
}
