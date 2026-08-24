using FinanceApp.Application.DTOs.Transfer;

namespace FinanceApp.Application.Interfaces;

public interface ITransferService
{
    Task<AccountTransferCreateResultDto> CreateAsync(
        Guid userId, AccountTransferCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountTransferSummaryDto>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<AccountTransferDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task CancelAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default);
}
