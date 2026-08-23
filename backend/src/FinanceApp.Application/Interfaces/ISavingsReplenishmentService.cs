using FinanceApp.Application.DTOs.SavingsGoal;

namespace FinanceApp.Application.Interfaces;

public interface ISavingsReplenishmentService
{
    Task<SavingsReplenishmentDto> CreateAsync(
        Guid userId, SavingsReplenishmentCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta el débito automático del ciclo para todos los planes
    /// Active + AutoDebitEnabled + no pausados del usuario. Idempotente:
    /// un plan ya debitado dentro del ciclo actual se cuenta en
    /// SkippedAlreadyDebitedCount y no se vuelve a procesar.
    /// </summary>
    Task<ReplenishmentCycleResultDto> ExecuteCycleDebitsAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<SavingsReplenishmentDto> ApplyManualDebitAsync(
        Guid userId, Guid replenishmentId, SavingsReplenishmentManualDebitDto dto,
        CancellationToken cancellationToken = default);

    Task<SavingsReplenishmentDto> PauseAsync(
        Guid userId, Guid replenishmentId, SavingsReplenishmentPauseDto dto,
        CancellationToken cancellationToken = default);

    Task<SavingsReplenishmentDto> ResumeAsync(
        Guid userId, Guid replenishmentId, CancellationToken cancellationToken = default);

    Task CancelAsync(
        Guid userId, Guid replenishmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavingsReplenishmentDto>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavingsReplenishmentDto>> GetByGoalIdAsync(
        Guid userId, Guid goalId, CancellationToken cancellationToken = default);

    Task<SavingsReplenishmentDto> GetByIdAsync(
        Guid userId, Guid replenishmentId, CancellationToken cancellationToken = default);
}
