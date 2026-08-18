using FinanceApp.Application.DTOs.CreditCard;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Enums;

namespace FinanceApp.UnitTests;

internal sealed record RecordedCardExpense(
    Guid UserId, Guid CreditCardId, Guid ExpenseId, decimal Amount,
    DateOnly Date, CreditCardTransactionType Type);

internal sealed class RecordingCreditCardService : ICreditCardService
{
    public List<RecordedCardExpense> SyncedExpenses { get; } = [];
    public List<Guid> RemovedExpenses { get; } = [];
    public bool IsActive { get; set; } = true;

    public Task<CreditCardResponseDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CreditCardResponseDto { Id = id, Name = "Tarjeta prueba", IsActive = IsActive });

    public Task SyncExpenseAsync(
        Guid userId, Guid creditCardId, Guid expenseId, decimal amount,
        DateOnly date, string description,
        CreditCardTransactionType type = CreditCardTransactionType.Purchase,
        CancellationToken cancellationToken = default)
    {
        if (!IsActive)
            throw new InvalidOperationException("Tarjeta inactiva");
        SyncedExpenses.Add(new(userId, creditCardId, expenseId, amount, date, type));
        return Task.CompletedTask;
    }

    public Task SyncReimbursementAsync(
        Guid userId, Guid creditCardId, Guid reimbursementId, decimal amount,
        DateOnly date, string description, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveExpenseAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default)
    {
        RemovedExpenses.Add(expenseId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CreditCardResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CreditCardResponseDto>>([]);
    public Task<CreditCardResponseDto> CreateAsync(Guid userId, CreditCardCreateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<CreditCardResponseDto> UpdateAsync(Guid id, Guid userId, CreditCardUpdateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<CreditCardTransactionResponseDto>> GetTransactionsAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CreditCardTransactionResponseDto>>([]);
    public Task<IReadOnlyList<CreditCardPaymentResponseDto>> GetPaymentsAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CreditCardPaymentResponseDto>>([]);
    public Task<CreditCardPaymentResponseDto> AddPaymentAsync(Guid id, Guid userId, CreditCardPaymentCreateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<CreditCardPaymentResponseDto> VoidPaymentAsync(Guid id, Guid paymentId, Guid userId, CreditCardPaymentVoidDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<CreditCardTransactionResponseDto> AddChargeAsync(Guid id, Guid userId, CreditCardChargeCreateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class PassThroughUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) => action(cancellationToken);
}
