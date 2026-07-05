using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Income;

namespace FinanceApp.Application.Interfaces;

public interface IIncomeService
{
    Task<PagedResponse<IncomeResponseDto>> GetAllAsync(
        Guid userId,
        IncomeFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<IncomeResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IncomeResponseDto> CreateAsync(
        Guid userId,
        IncomeCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<IncomeResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        IncomeUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IncomeSummaryDto> GetSummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);
}