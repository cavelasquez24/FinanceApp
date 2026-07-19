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

    /// <summary>
    /// v2.0.1 — Retira dinero de una meta de ahorro. Reduce CurrentAmount
    /// (Math.Max(0, ...)). NO crea Expense: si Reason = Consumed, el
    /// LinkedExpenseId (si viene) solo se registra como referencia —
    /// la creación del Expense es responsabilidad de otro flujo/módulo.
    /// </summary>
    Task<SavingsGoalWithdrawalResponseDto> WithdrawAsync(
        Guid id,
        Guid userId,
        SavingsGoalWithdrawalCreateDto dto,
        CancellationToken cancellationToken = default);
}
