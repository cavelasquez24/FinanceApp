using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.UnitTests;

internal sealed class EmptyDebtRepository : IDebtRepository
{
    public Task<IEnumerable<Debt>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Debt>>([]);

    public Task<decimal> GetTotalCurrentBalanceAsync(
        Guid userId,
        CancellationToken cancellationToken = default) => Task.FromResult(0m);

    public Task<decimal> GetTotalPaymentsByDateRangeAsync(
        Guid userId,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default) => Task.FromResult(0m);

    public Task<decimal> GetTotalPrincipalPaidByDateRangeAsync(
        Guid userId,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default) => Task.FromResult(0m);

    public Task AddWithdrawalAsync(
        DebtWithdrawal withdrawal,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<Debt?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) => Task.FromResult<Debt?>(null);

    public Task<IEnumerable<Debt>> GetAllAsync(
        CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Debt>>([]);

    public Task<Debt> CreateAsync(
        Debt entity,
        CancellationToken cancellationToken = default) => Task.FromResult(entity);

    public Task<Debt> UpdateAsync(
        Debt entity,
        CancellationToken cancellationToken = default) => Task.FromResult(entity);

    public Task DeleteAsync(
        Debt entity,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
