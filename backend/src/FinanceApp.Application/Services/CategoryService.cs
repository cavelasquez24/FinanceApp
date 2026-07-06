using FinanceApp.Application.DTOs.Category;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync(
        Guid userId,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        CategoryType? categoryType = type != null
            ? Enum.Parse<CategoryType>(type, ignoreCase: true)
            : null;

        var categories = await _categoryRepository
            .GetByUserIdAsync(userId, categoryType, cancellationToken);

        return categories.Select(MapToResponseDto);
    }

    public async Task<CategoryResponseDto> CreateAsync(
        Guid userId,
        CategoryCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var categoryType = Enum.Parse<CategoryType>(dto.Type, ignoreCase: true);

        // Verificar que no existe una categoría con el mismo nombre y tipo
        var exists = await _categoryRepository.ExistsByNameAsync(
            userId, dto.Name, categoryType, cancellationToken);

        if (exists)
            throw new DomainException(
                "CATEGORY_ALREADY_EXISTS",
                $"Ya existe una categoría llamada '{dto.Name}' de tipo '{dto.Type}'");

        var category = new Category
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Type = categoryType,
            Icon = dto.Icon?.Trim(),
            Color = dto.Color,
            IsDefault = false
        };

        await _categoryRepository.CreateAsync(category, cancellationToken);
        return MapToResponseDto(category);
    }

    public async Task<CategoryResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        CategoryUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);

        // No existe, no pertenece al usuario o es una categoría del sistema
        if (category == null || category.UserId != userId || category.IsDeleted)
            throw new NotFoundException("Categoría", id);

        // Las categorías del sistema no se pueden editar
        if (category.IsDefault)
            throw new DomainException(
                "CANNOT_MODIFY_DEFAULT_CATEGORY",
                "Las categorías del sistema no pueden modificarse");

        category.Name = dto.Name.Trim();
        category.Icon = dto.Icon?.Trim();
        category.Color = dto.Color;

        await _categoryRepository.UpdateAsync(category, cancellationToken);
        return MapToResponseDto(category);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);

        if (category == null || category.UserId != userId || category.IsDeleted)
            throw new NotFoundException("Categoría", id);

        if (category.IsDefault)
            throw new DomainException(
                "CANNOT_DELETE_DEFAULT_CATEGORY",
                "Las categorías del sistema no pueden eliminarse");

        category.DeletedAt = DateTimeOffset.UtcNow;
        await _categoryRepository.UpdateAsync(category, cancellationToken);
    }

    private static CategoryResponseDto MapToResponseDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Type = category.Type.ToString().ToLower(),
        Icon = category.Icon,
        Color = category.Color,
        IsDefault = category.IsDefault
    };
}