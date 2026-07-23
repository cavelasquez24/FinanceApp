using FinanceApp.Application.DTOs.Debt;

namespace FinanceApp.Application.Interfaces;

public interface IDebtService
{
    Task<IEnumerable<DebtResponseDto>> GetAllAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<DebtResponseDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<DebtResponseDto> CreateAsync(
        Guid userId, DebtCreateDto dto, CancellationToken cancellationToken = default);

    Task<DebtResponseDto> UpdateAsync(
        Guid id, Guid userId, DebtUpdateDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<DebtSummaryDto> GetSummaryAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<DebtPaymentResponseDto>> GetPaymentsAsync(
        Guid debtId, Guid userId, CancellationToken cancellationToken = default);

    Task<DebtPaymentResponseDto> AddPaymentAsync(
        Guid debtId, Guid userId, DebtPaymentCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<DebtWithdrawalResponseDto> AddWithdrawalAsync(
        Guid debtId, Guid userId, DebtWithdrawalCreateDto dto,
        CancellationToken cancellationToken = default);
}