using FinanceApp.Application.DTOs.Account;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces;

public interface IFinancialAccountService
{
    Task<IReadOnlyList<FinancialAccountResponseDto>> GetAllAsync(
        Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountTransactionResponseDto>> GetRecentTransactionsAsync(
        Guid userId, int count, CancellationToken cancellationToken = default);
    Task<FinancialAccountResponseDto> CreateAsync(
        Guid userId, FinancialAccountCreateDto dto,
        CancellationToken cancellationToken = default);
    Task<FinancialAccountResponseDto> UpdateAsync(
        Guid id, Guid userId, FinancialAccountUpdateDto dto,
        CancellationToken cancellationToken = default);
    Task<FinancialAccountResponseDto> GetOrCreateDefaultAsync(
        Guid userId, FinancialAccountType type,
        CancellationToken cancellationToken = default);
    Task SyncMovementAsync(
        Guid userId, Guid? accountId, FinancialAccountType fallbackType,
        decimal signedAmount, DateOnly date, string sourceType, Guid sourceId,
        string description, CancellationToken cancellationToken = default);
    Task SyncTransferAsync(
        Guid userId, FinancialAccountType fromType, FinancialAccountType toType,
        decimal amount, DateOnly date, string sourceType, Guid sourceId,
        string description, CancellationToken cancellationToken = default);
}
