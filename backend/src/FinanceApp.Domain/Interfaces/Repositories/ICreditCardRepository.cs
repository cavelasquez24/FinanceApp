using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface ICreditCardRepository : IBaseRepository<CreditCard>
{
    Task<IReadOnlyList<CreditCard>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CreditCard?> GetByIdWithHistoryAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<CreditCardTransaction?> GetTransactionBySourceAsync(Guid userId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default);
    Task<CreditCardPayment?> GetPaymentByIdempotencyKeyAsync(Guid userId, Guid creditCardId, Guid idempotencyKey, CancellationToken cancellationToken = default);
    Task<CreditCardPayment?> GetPaymentByIdAsync(Guid paymentId, Guid creditCardId, Guid userId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalCurrentBalanceAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPrincipalPaidByDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task SaveTransactionAsync(CreditCardTransaction transaction, CancellationToken cancellationToken = default);
    Task SavePaymentAsync(CreditCardPayment payment, CancellationToken cancellationToken = default);
}
