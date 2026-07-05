using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Income;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class IncomeService : IIncomeService
{
    private readonly IIncomeRepository _incomeRepository;

    public IncomeService(IIncomeRepository incomeRepository)
    {
        _incomeRepository = incomeRepository;
    }

    public async Task<PagedResponse<IncomeResponseDto>> GetAllAsync(
        Guid userId,
        IncomeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _incomeRepository.GetByUserIdAsync(
            userId,
            filter.Page,
            filter.PageSize,
            filter.CategoryId,
            filter.StartDate,
            filter.EndDate,
            cancellationToken);

        return new PagedResponse<IncomeResponseDto>
        {
            Items = items.Select(MapToResponseDto),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IncomeResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var income = await _incomeRepository.GetByIdAsync(id, cancellationToken);

        // Si no existe O no pertenece al usuario → 404
        // Nunca revelamos si el recurso existe pero pertenece a otro usuario
        if (income == null || income.UserId != userId || income.IsDeleted)
            throw new NotFoundException("Ingreso", id);

        return MapToResponseDto(income);
    }

    public async Task<IncomeResponseDto> CreateAsync(
        Guid userId,
        IncomeCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var income = new Income
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            Amount = dto.Amount,
            Description = dto.Description?.Trim(),
            Date = dto.Date,
            Source = dto.Source?.Trim()
        };

        await _incomeRepository.CreateAsync(income, cancellationToken);

        // Recargamos con la categoría incluida para el response
        return await GetByIdAsync(income.Id, userId, cancellationToken);
    }

    public async Task<IncomeResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        IncomeUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var income = await _incomeRepository.GetByIdAsync(id, cancellationToken);

        if (income == null || income.UserId != userId || income.IsDeleted)
            throw new NotFoundException("Ingreso", id);

        // Actualizamos solo los campos del DTO
        income.CategoryId = dto.CategoryId;
        income.Amount = dto.Amount;
        income.Description = dto.Description?.Trim();
        income.Date = dto.Date;
        income.Source = dto.Source?.Trim();

        await _incomeRepository.UpdateAsync(income, cancellationToken);

        return await GetByIdAsync(income.Id, userId, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var income = await _incomeRepository.GetByIdAsync(id, cancellationToken);

        if (income == null || income.UserId != userId || income.IsDeleted)
            throw new NotFoundException("Ingreso", id);

        // Soft delete: marcamos como eliminado, no borramos el registro
        income.DeletedAt = DateTimeOffset.UtcNow;
        await _incomeRepository.UpdateAsync(income, cancellationToken);
    }

    public async Task<IncomeSummaryDto> GetSummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _incomeRepository.GetByUserIdAsync(
            userId,
            page: 1,
            pageSize: int.MaxValue, // todos para calcular el resumen
            startDate: new DateOnly(year, month, 1),
            endDate: new DateOnly(year, month,
                DateTime.DaysInMonth(year, month)),
            cancellationToken: cancellationToken);

        var incomeList = items.ToList();
        var totalAmount = incomeList.Sum(i => i.Amount);

        // por categoría para  gráfico
        var byCategory = incomeList
            .GroupBy(i => new
            {
                i.CategoryId,
                i.Category.Name,
                i.Category.Color
            })
            .Select(g => new IncomeByCategoryDto
            {
                CategoryName = g.Key.Name,
                CategoryColor = g.Key.Color,
                Amount = g.Sum(i => i.Amount),
                Percentage = totalAmount > 0
                    ? Math.Round(g.Sum(i => i.Amount) * 100 / totalAmount, 2)
                    : 0
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new IncomeSummaryDto
        {
            TotalAmount = totalAmount,
            TotalCount = totalCount,
            ByCategory = byCategory
        };
    }

    /// <summary>
    /// Convierte una entidad Income a su DTO de respuesta.
    /// Método privado reutilizado en todos los métodos del servicio.
    /// </summary>
    private static IncomeResponseDto MapToResponseDto(Income income) => new()
    {
        Id = income.Id,
        CategoryId = income.CategoryId,
        CategoryName = income.Category?.Name ?? string.Empty,
        CategoryColor = income.Category?.Color ?? string.Empty,
        CategoryIcon = income.Category?.Icon,
        Amount = income.Amount,
        Description = income.Description,
        Date = income.Date,
        Source = income.Source,
        CreatedAt = income.CreatedAt
    };
}