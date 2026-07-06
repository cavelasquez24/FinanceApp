using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Expense;

namespace FinanceApp.Application.Interfaces;

public interface IExpenseService
{
    Task<PagedResponse<ExpenseResponseDto>> GetAllAsync(
        Guid userId,
        ExpenseFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<ExpenseResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ExpenseResponseDto> CreateAsync(
        Guid userId,
        ExpenseCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<ExpenseResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        ExpenseUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ExpenseSummaryDto> GetSummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);
}