using FinanceApp.Application.DTOs.Account;

namespace FinanceApp.Application.Interfaces;

public interface IAccountReconciliationService
{
    Task<ReconciliationPreviewDto> GetPreviewAsync(
        Guid accountId, Guid userId,
        CancellationToken cancellationToken = default);

    Task<ReconciliationResponseDto> ApplyAsync(
        Guid accountId, Guid userId, ReconciliationCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReconciliationResponseDto>> GetHistoryAsync(
        Guid accountId, Guid userId, int page, int pageSize,
        CancellationToken cancellationToken = default);
}
