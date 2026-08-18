using FinanceApp.Application.Services;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.UnitTests;

public class FinancialAccountMovementTests
{
    [Fact]
    public async Task ExpenseMovement_CreateChangeAccountAndDelete_LeavesNoDuplicateBalance()
    {
        var userId = Guid.NewGuid();
        var cash = new FinancialAccount { Id = Guid.NewGuid(), UserId = userId, Type = FinancialAccountType.Cash, CurrentBalance = 100m, IsActive = true };
        var wallet = new FinancialAccount { Id = Guid.NewGuid(), UserId = userId, Type = FinancialAccountType.Cash, CurrentBalance = 100m, IsActive = true };
        var repository = new FakeFinancialAccountRepository(cash, wallet);
        var service = new FinancialAccountService(repository, null!, null!, new PassThroughUnitOfWork());
        var expenseId = Guid.NewGuid();

        await service.SyncMovementAsync(userId, cash.Id, FinancialAccountType.Cash, -20m, new DateOnly(2026, 8, 1), "expense", expenseId, "Gasolina");
        Assert.Equal(80m, cash.CurrentBalance);
        Assert.Single(repository.Transactions);

        await service.SyncMovementAsync(userId, wallet.Id, FinancialAccountType.Cash, -35m, new DateOnly(2026, 8, 2), "expense", expenseId, "Gasolina");
        Assert.Equal(100m, cash.CurrentBalance);
        Assert.Equal(65m, wallet.CurrentBalance);
        var movement = Assert.Single(repository.Transactions);
        Assert.Equal(wallet.Id, movement.AccountId);
        Assert.Equal(-35m, movement.Amount);
        Assert.Equal(new DateOnly(2026, 8, 2), movement.Date);

        wallet.IsActive = false;
        await service.SyncMovementAsync(userId, wallet.Id, FinancialAccountType.Cash, 0m, new DateOnly(2026, 8, 2), "expense", expenseId, "Gasto eliminado");
        Assert.Equal(100m, wallet.CurrentBalance);
        Assert.True(movement.IsDeleted);
    }

    private sealed class FakeFinancialAccountRepository : IFinancialAccountRepository
    {
        public List<FinancialAccount> Accounts { get; } = [];
        public List<AccountTransaction> Transactions { get; } = [];
        public FakeFinancialAccountRepository(params FinancialAccount[] accounts) => Accounts.AddRange(accounts);
        public Task<FinancialAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Accounts.SingleOrDefault(a => a.Id == id));
        public Task<IEnumerable<FinancialAccount>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FinancialAccount>>(Accounts);
        public Task<FinancialAccount> CreateAsync(FinancialAccount entity, CancellationToken cancellationToken = default) { Accounts.Add(entity); return Task.FromResult(entity); }
        public Task<FinancialAccount> UpdateAsync(FinancialAccount entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task DeleteAsync(FinancialAccount entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FinancialAccount>>(Accounts.Where(a => a.UserId == userId).ToList());
        public Task<FinancialAccount?> GetDefaultAsync(Guid userId, FinancialAccountType type, CancellationToken cancellationToken = default) => Task.FromResult(Accounts.SingleOrDefault(a => a.UserId == userId && a.Type == type && a.IsDefault && a.IsActive && !a.IsDeleted));
        public Task<AccountTransaction?> GetTransactionBySourceAsync(Guid userId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.SingleOrDefault(t => t.UserId == userId && t.SourceType == sourceType && t.SourceId == sourceId && !t.IsDeleted));
        public Task<IReadOnlyList<AccountTransaction>> GetRecentTransactionsAsync(Guid userId, int count, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountTransaction>>([]);
        public Task<(decimal OpeningBalances, decimal Adjustments)> GetOpeningAndAdjustmentTotalsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult((Transactions.Where(t => t.UserId == userId && t.SourceType == "account-opening").Sum(t => t.Amount), Transactions.Where(t => t.UserId == userId && t.SourceType == "account-adjustment").Sum(t => t.Amount)));
        public Task<IReadOnlyList<AccountTransaction>> GetTransactionsByTransferIdAsync(Guid userId, Guid transferId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountTransaction>>(Transactions.Where(t => t.UserId == userId && t.TransferId == transferId).ToList());
        public Task SaveTransactionAsync(AccountTransaction transaction, CancellationToken cancellationToken = default) { if (!Transactions.Contains(transaction)) Transactions.Add(transaction); return Task.CompletedTask; }
        public Task DeleteTransactionAsync(AccountTransaction transaction, CancellationToken cancellationToken = default) { transaction.DeletedAt = DateTimeOffset.UtcNow; return Task.CompletedTask; }
    }

    private sealed class PassThroughUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) => action(cancellationToken);
    }
}
