using System.Security.Claims;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Tag;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    public TagsController(ITagService tagService) => _tagService = tagService;

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await _tagService.GetAllAsync(GetUserId(), search, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TagResponseDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TagCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _tagService.CreateAsync(GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<TagResponseDto>.Ok(result, "Etiqueta creada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TagUpdateDto dto, CancellationToken cancellationToken)
    {
        var result = await _tagService.UpdateAsync(id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<TagResponseDto>.Ok(result, "Etiqueta actualizada exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _tagService.DeleteAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Etiqueta eliminada exitosamente"));
    }

    [HttpPost("{sourceId:guid}/merge")]
    public async Task<IActionResult> Merge(Guid sourceId, [FromBody] TagMergeDto dto, CancellationToken cancellationToken)
    {
        var result = await _tagService.MergeAsync(sourceId, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<TagResponseDto>.Ok(result, "Etiquetas fusionadas exitosamente"));
    }

    [HttpGet("expense-report")]
    public async Task<IActionResult> GetExpenseReport(
        [FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, CancellationToken cancellationToken)
    {
        var result = await _tagService.GetExpenseReportAsync(GetUserId(), startDate, endDate, cancellationToken);
        return Ok(ApiResponse<TagExpenseReportDto>.Ok(result));
    }
}
