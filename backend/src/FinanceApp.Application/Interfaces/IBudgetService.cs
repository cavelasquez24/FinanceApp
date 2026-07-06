using FinanceApp.Application.DTOs.Budget;

namespace FinanceApp.Application.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<BudgetResponseDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<BudgetResponseDto?> GetCurrentAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<BudgetResponseDto?> GetByPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task<BudgetResponseDto> CreateAsync(
        Guid userId,
        BudgetCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<BudgetResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        BudgetUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task<BudgetStatusDto> GetStatusAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}