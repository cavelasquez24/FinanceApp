using FinanceApp.Application.DTOs.Category;

namespace FinanceApp.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponseDto>> GetAllAsync(
        Guid userId,
        string? type = null,
        CancellationToken cancellationToken = default);

    Task<CategoryResponseDto> CreateAsync(
        Guid userId,
        CategoryCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<CategoryResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        CategoryUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}