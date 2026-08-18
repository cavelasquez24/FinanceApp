using System.Security.Claims;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.CreditCard;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/credit-cards")]
[Authorize]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _service;

    public CreditCardsController(ICreditCardService service) => _service = service;

    private Guid GetUserId() => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<CreditCardResponseDto>>.Ok(
            await _service.GetAllAsync(GetUserId(), cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CreditCardResponseDto>.Ok(
            await _service.GetByIdAsync(id, GetUserId(), cancellationToken)));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreditCardCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<CreditCardResponseDto>.Ok(
            result, "Tarjeta registrada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] CreditCardUpdateDto dto,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<CreditCardResponseDto>.Ok(
            await _service.UpdateAsync(id, GetUserId(), dto, cancellationToken),
            "Tarjeta actualizada exitosamente"));

    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<CreditCardTransactionResponseDto>>.Ok(
            await _service.GetTransactionsAsync(id, GetUserId(), cancellationToken)));

    [HttpGet("{id:guid}/payments")]
    public async Task<IActionResult> GetPayments(
        Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<CreditCardPaymentResponseDto>>.Ok(
            await _service.GetPaymentsAsync(id, GetUserId(), cancellationToken)));

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> AddPayment(
        Guid id, [FromBody] CreditCardPaymentCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.AddPaymentAsync(id, GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<CreditCardPaymentResponseDto>.Ok(
            result, "Pago de tarjeta registrado exitosamente"));
    }
    [HttpPost("{id:guid}/payments/{paymentId:guid}/void")]
    public async Task<IActionResult> VoidPayment(
        Guid id, Guid paymentId, [FromBody] CreditCardPaymentVoidDto dto,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<CreditCardPaymentResponseDto>.Ok(
            await _service.VoidPaymentAsync(
                id, paymentId, GetUserId(), dto, cancellationToken),
            "Pago anulado y reversado exitosamente"));


    [HttpPost("{id:guid}/charges")]
    public async Task<IActionResult> AddCharge(
        Guid id, [FromBody] CreditCardChargeCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.AddChargeAsync(id, GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<CreditCardTransactionResponseDto>.Ok(
            result, "Cargo financiero registrado exitosamente"));
    }
}
