using FinanceApp.Application.DTOs.CreditCard;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces;

public interface ICreditCardService
{
    Task<IReadOnlyList<CreditCardResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CreditCardResponseDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<CreditCardResponseDto> CreateAsync(Guid userId, CreditCardCreateDto dto, CancellationToken cancellationToken = default);
    Task<CreditCardResponseDto> UpdateAsync(Guid id, Guid userId, CreditCardUpdateDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CreditCardTransactionResponseDto>> GetTransactionsAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CreditCardPaymentResponseDto>> GetPaymentsAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<CreditCardPaymentResponseDto> AddPaymentAsync(Guid id, Guid userId, CreditCardPaymentCreateDto dto, CancellationToken cancellationToken = default);
    Task<CreditCardPaymentResponseDto> VoidPaymentAsync(Guid id, Guid paymentId, Guid userId, CreditCardPaymentVoidDto dto, CancellationToken cancellationToken = default);
    Task<CreditCardTransactionResponseDto> AddChargeAsync(Guid id, Guid userId, CreditCardChargeCreateDto dto, CancellationToken cancellationToken = default);
    Task SyncExpenseAsync(Guid userId, Guid creditCardId, Guid expenseId, decimal amount, DateOnly date, string description, CreditCardTransactionType type = CreditCardTransactionType.Purchase, CancellationToken cancellationToken = default);
    Task RemoveExpenseAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default);
    Task SyncReimbursementAsync(Guid userId, Guid creditCardId, Guid reimbursementId,
        decimal amount, DateOnly date, string description,
        CancellationToken cancellationToken = default);
}
