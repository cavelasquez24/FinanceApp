using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.UnitTests;

public class FinancialAccountTransferTests
{
    [Fact]
    public async Task Transfer_MovesBothBalances_WithoutChangingTotalBalance()
    {
        var fixture = new TransferFixture(100m, 5m);
        var key = Guid.NewGuid();

        var result = await fixture.Service.TransferAsync(fixture.UserId, Transfer(fixture, 20m, key));

        Assert.Equal(key, result.TransferId);
        Assert.Equal(80m, fixture.From.CurrentBalance);
        Assert.Equal(25m, fixture.To.CurrentBalance);
        Assert.Equal(105m, fixture.Repository.Accounts.Sum(account => account.CurrentBalance));
        Assert.Equal(2, fixture.Repository.Transactions.Count);
        Assert.Equal(0m, fixture.Repository.Transactions.Sum(transaction => transaction.Amount));
        Assert.All(fixture.Repository.Transactions, transaction => Assert.Equal(key, transaction.TransferId));
        Assert.Contains(fixture.Repository.Transactions, transaction => transaction.Amount == -20m);
        Assert.Contains(fixture.Repository.Transactions, transaction => transaction.Amount == 20m);
    }

    [Fact]
    public async Task Transfer_WithSameIdempotencyKey_DoesNotDuplicateMovements()
    {
        var fixture = new TransferFixture(100m, 0m);
        var request = Transfer(fixture, 20m, Guid.NewGuid());

        var first = await fixture.Service.TransferAsync(fixture.UserId, request);
        var second = await fixture.Service.TransferAsync(fixture.UserId, request);

        Assert.Equal(first.TransferId, second.TransferId);
        Assert.Equal(80m, fixture.From.CurrentBalance);
        Assert.Equal(20m, fixture.To.CurrentBalance);
        Assert.Equal(2, fixture.Repository.Transactions.Count);
    }

    [Fact]
    public async Task Transfer_WhenSecondLegFails_RollsBackBothBalancesAndMovements()
    {
        var fixture = new TransferFixture(100m, 0m) { Repository = { FailOnTransactionSave = 2 } };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.TransferAsync(fixture.UserId, Transfer(fixture, 20m, Guid.NewGuid())));

        Assert.Equal(100m, fixture.From.CurrentBalance);
        Assert.Equal(0m, fixture.To.CurrentBalance);
        Assert.Empty(fixture.Repository.Transactions);
    }

    [Fact]
    public async Task Transfer_RejectsAccountOwnedByAnotherUser()
    {
        var fixture = new TransferFixture(100m, 0m);
        fixture.To.UserId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            fixture.Service.TransferAsync(fixture.UserId, Transfer(fixture, 20m, Guid.NewGuid())));

        Assert.Empty(fixture.Repository.Transactions);
        Assert.Equal(100m, fixture.From.CurrentBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Transfer_RejectsNonPositiveAmount(decimal amount)
    {
        var fixture = new TransferFixture(100m, 0m);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            fixture.Service.TransferAsync(fixture.UserId, Transfer(fixture, amount, Guid.NewGuid())));

        Assert.Equal("INVALID_TRANSFER_AMOUNT", error.Code);
    }

    [Fact]
    public async Task Transfer_RejectsInsufficientSourceBalance()
    {
        var fixture = new TransferFixture(10m, 0m);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            fixture.Service.TransferAsync(fixture.UserId, Transfer(fixture, 20m, Guid.NewGuid())));

        Assert.Equal("INSUFFICIENT_ACCOUNT_BALANCE", error.Code);
        Assert.Empty(fixture.Repository.Transactions);
    }

    private static AccountTransferCreateDto Transfer(TransferFixture fixture, decimal amount, Guid key) => new()
    {
        FromAccountId = fixture.From.Id,
        ToAccountId = fixture.To.Id,
        Amount = amount,
        Date = new DateOnly(2026, 8, 16),
        Description = "Operativa",
        IdempotencyKey = key
    };

    private sealed class TransferFixture
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public FinancialAccount From { get; }
        public FinancialAccount To { get; }
        public FakeAccountRepository Repository { get; }
        public FinancialAccountService Service { get; }

        public TransferFixture(decimal fromBalance, decimal toBalance)
        {
            From = new FinancialAccount { Id = Guid.NewGuid(), UserId = UserId, Name = "Efectivo", CurrentBalance = fromBalance, IsActive = true };
            To = new FinancialAccount { Id = Guid.NewGuid(), UserId = UserId, Name = "Operativa", CurrentBalance = toBalance, IsActive = true };
            Repository = new FakeAccountRepository([From, To]);
            Service = new FinancialAccountService(Repository, null!, null!, new RollbackUnitOfWork(Repository));
        }
    }

    private sealed class RollbackUnitOfWork(FakeAccountRepository repository) : IUnitOfWork
    {
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        {
            var snapshot = repository.Snapshot();
            try { return await action(cancellationToken); }
            catch { repository.Restore(snapshot); throw; }
        }
    }

    private sealed class FakeAccountRepository(IEnumerable<FinancialAccount> accounts) : IFinancialAccountRepository
    {
        public List<FinancialAccount> Accounts { get; } = accounts.ToList();
        public List<AccountTransaction> Transactions { get; } = new();
        public int FailOnTransactionSave { get; set; }
        private int _transactionSaves;

        public (Dictionary<Guid, decimal> Balances, List<AccountTransaction> Transactions) Snapshot() =>
            (Accounts.ToDictionary(account => account.Id, account => account.CurrentBalance), Transactions.Select(Clone).ToList());
        public void Restore((Dictionary<Guid, decimal> Balances, List<AccountTransaction> Transactions) snapshot)
        {
            foreach (var account in Accounts) account.CurrentBalance = snapshot.Balances[account.Id];
            Transactions.Clear();
            Transactions.AddRange(snapshot.Transactions);
            _transactionSaves = 0;
        }

        public Task<FinancialAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Accounts.SingleOrDefault(account => account.Id == id));
        public Task<IEnumerable<FinancialAccount>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FinancialAccount>>(Accounts);
        public Task<FinancialAccount> CreateAsync(FinancialAccount entity, CancellationToken cancellationToken = default) { Accounts.Add(entity); return Task.FromResult(entity); }
        public Task<FinancialAccount> UpdateAsync(FinancialAccount entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task DeleteAsync(FinancialAccount entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FinancialAccount>>(Accounts.Where(account => account.UserId == userId).ToList());
        public Task<FinancialAccount?> GetDefaultAsync(Guid userId, FinanceApp.Domain.Enums.FinancialAccountType type, CancellationToken cancellationToken = default) => Task.FromResult<FinancialAccount?>(null);
        public Task<AccountTransaction?> GetTransactionBySourceAsync(Guid userId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.SingleOrDefault(transaction => transaction.UserId == userId && transaction.SourceType == sourceType && transaction.SourceId == sourceId));
        public Task<IReadOnlyList<AccountTransaction>> GetTransactionsByTransferIdAsync(Guid userId, Guid transferId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountTransaction>>(Transactions.Where(transaction => transaction.UserId == userId && transaction.TransferId == transferId).ToList());
        public Task<IReadOnlyList<AccountTransaction>> GetRecentTransactionsAsync(Guid userId, int count, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountTransaction>>(Transactions.Where(transaction => transaction.UserId == userId).Take(count).ToList());
        public Task<(decimal OpeningBalances, decimal Adjustments)> GetOpeningAndAdjustmentTotalsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult((Transactions.Where(t => t.UserId == userId && t.SourceType == "account-opening").Sum(t => t.Amount), Transactions.Where(t => t.UserId == userId && t.SourceType == "account-adjustment").Sum(t => t.Amount)));
        public Task SaveTransactionAsync(AccountTransaction transaction, CancellationToken cancellationToken = default)
        {
            _transactionSaves++;
            if (FailOnTransactionSave == _transactionSaves) throw new InvalidOperationException("fallo simulado");
            transaction.Id = transaction.Id == Guid.Empty ? Guid.NewGuid() : transaction.Id;
            transaction.Account = Accounts.Single(account => account.Id == transaction.AccountId);
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }
        public Task DeleteTransactionAsync(AccountTransaction transaction, CancellationToken cancellationToken = default) { Transactions.Remove(transaction); return Task.CompletedTask; }
        private static AccountTransaction Clone(AccountTransaction transaction) => new()
        {
            Id = transaction.Id, UserId = transaction.UserId, AccountId = transaction.AccountId, Amount = transaction.Amount,
            Date = transaction.Date, Description = transaction.Description, SourceType = transaction.SourceType,
            SourceId = transaction.SourceId, TransferId = transaction.TransferId, Account = transaction.Account
        };
    }
}
