using FinanceApp.Application.DTOs.SavingsGoal;

namespace FinanceApp.Application.Interfaces;

public interface ISavingsGoalService
{
    Task<IEnumerable<SavingsGoalResponseDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SavingsGoalResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SavingsGoalResponseDto> CreateAsync(
        Guid userId,
        SavingsGoalCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<SavingsGoalResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        SavingsGoalUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SavingsGoalResponseDto> DepositAsync(
        Guid id,
        Guid userId,
        DepositDto dto,
        CancellationToken cancellationToken = default);
}