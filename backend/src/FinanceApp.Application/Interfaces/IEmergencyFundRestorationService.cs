using FinanceApp.Application.DTOs.SavingsGoal;

namespace FinanceApp.Application.Interfaces;

public interface IEmergencyFundRestorationService
{
    Task<IReadOnlyList<EmergencyFundRestorationResponseDto>> GetByGoalAsync(
        Guid goalId, Guid userId, CancellationToken cancellationToken = default);
    Task<EmergencyFundRestorationResponseDto> CreateUseAsync(
        Guid goalId, Guid userId, EmergencyFundUseCreateDto dto,
        CancellationToken cancellationToken = default);
    Task<EmergencyFundRestorationResponseDto> RegisterPaymentAsync(
        Guid restorationId, Guid userId, EmergencyFundRestorationPaymentDto dto,
        CancellationToken cancellationToken = default);
    Task<EmergencyFundRestorationResponseDto> CancelAsync(
        Guid restorationId, Guid userId, CancellationToken cancellationToken = default);
}
