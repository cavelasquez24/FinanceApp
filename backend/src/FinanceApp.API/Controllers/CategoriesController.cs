using FinanceApp.Application.DTOs.Category;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllAsync(
            GetUserId(), type, cancellationToken);
        return Ok(ApiResponse<IEnumerable<CategoryResponseDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CategoryCreateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(
            GetUserId(), dto, cancellationToken);
        return StatusCode(201, ApiResponse<CategoryResponseDto>.Ok(
            result, "Categoría creada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CategoryUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(
            id, GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<CategoryResponseDto>.Ok(
            result, "Categoría actualizada exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, GetUserId(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Categoría eliminada exitosamente"));
    }
}