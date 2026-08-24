using FinanceApp.Application.DTOs.Transfer;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.UnitTests;

public class TransferServiceTests
{
    private sealed class Fixture
    {
        public FakeAccountTransferRepository TransferRepo { get; } = new();
        public FakeAccountRepository AccountRepo { get; }
        public MutableBusinessDateProvider DateProvider { get; } = new() { Today = new DateOnly(2026, 8, 23) };
        public IUnitOfWork UnitOfWork { get; }
        public TransferService Service { get; }

        public Fixture(FinancialAccount from, FinancialAccount to, bool withRollback = false)
        {
            AccountRepo = new FakeAccountRepository([from, to]);
            UnitOfWork = withRollback
                ? new RollbackUnitOfWork(TransferRepo, AccountRepo)
                : new PassThroughUnitOfWork();
            Service = new TransferService(TransferRepo, AccountRepo, UnitOfWork, DateProvider);
        }
    }

    private static FinancialAccount NewAccount(Guid userId, decimal balance, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = "Cuenta",
        CurrentBalance = balance,
        IsActive = isActive
    };

    private static AccountTransferCreateDto CreateDto(
        FinancialAccount from, FinancialAccount to, decimal amount, Guid? transferGroupId = null) => new()
    {
        FromAccountId = from.Id,
        ToAccountId = to.Id,
        Amount = amount,
        TransferDate = new DateOnly(2026, 8, 23),
        Description = "Operativa",
        TransferGroupId = transferGroupId
    };

    // 1. Transferencia exitosa: saldos se mueven exacto por Amount, patrimonio invariante.
    [Fact]
    public async Task CreateAsync_MovesBothBalances_WithoutChangingCombinedWealth()
    {
        var userId = Guid.NewGuid();
        var from = NewAccount(userId, 100m);
        var to = NewAccount(userId, 5m);
        var fx = new Fixture(from, to);

        var result = await fx.Service.CreateAsync(userId, CreateDto(from, to, 20m));

        Assert.Equal(80m, from.CurrentBalance);
        Assert.Equal(25m, to.CurrentBalance);
        Assert.Equal(105m, from.CurrentBalance + to.CurrentBalance);
        Assert.False(result.InsufficientFundsWarning);
        Assert.Equal("completed", result.Transfer.Status);
        Assert.Single(fx.TransferRepo.Items);
    }

    // 2. FromAccountId == ToAccountId → DomainException antes de tocar saldos.
    [Fact]
    public async Task CreateAsync_RejectsSameAccount_WithoutTouchingBalances()
    {
        var userId = Guid.NewGuid();
        var account = NewAccount(userId, 100m);
        var fx = new Fixture(account, account);

        var dto = CreateDto(account, account, 20m);
        var error = await Assert.ThrowsAsync<DomainException>(() => fx.Service.CreateAsync(userId, dto));

        Assert.Equal("SAME_TRANSFER_ACCOUNT", error.Code);
        Assert.Equal(100m, account.CurrentBalance);
        Assert.Empty(fx.TransferRepo.Items);
    }

    // 3. Cuenta no pertenece al usuario → NotFoundException, saldos sin cambio.
    [Fact]
    public async Task CreateAsync_RejectsAccountOwnedByAnotherUser()
    {
        var userId = Guid.NewGuid();
        var from = NewAccount(userId, 100m);
        var to = NewAccount(Guid.NewGuid(), 5m); // pertenece a otro usuario
        var fx = new Fixture(from, to);

        await Assert.ThrowsAsync<NotFoundException>(
            () => fx.Service.CreateAsync(userId, CreateDto(from, to, 20m)));

        Assert.Equal(100m, from.CurrentBalance);
        Assert.Equal(5m, to.CurrentBalance);
        Assert.Empty(fx.TransferRepo.Items);
    }

    // 4. Cuenta inactiva → DomainException, saldos sin cambio.
    [Fact]
    public async Task CreateAsync_RejectsInactiveAccount()
    {
        var userId = Guid.NewGuid();
        var from = NewAccount(userId, 100m);
        var to = NewAccount(userId, 5m, isActive: false);
        var fx = new Fixture(from, to);

        var error = await Assert.ThrowsAsync<DomainException>(
            () => fx.Service.CreateAsync(userId, CreateDto(from, to, 20m)));

        Assert.Equal("INACTIVE_ACCOUNT", error.Code);
        Assert.Equal(100m, from.CurrentBalance);
        Assert.Equal(5m, to.CurrentBalance);
        Assert.Empty(fx.TransferRepo.Items);
    }

    // 5. Saldo insuficiente → advertencia, no bloqueo; la transferencia se ejecuta igual.
    [Fact]
    public async Task CreateAsync_InsufficientBalance_ExecutesAnywayWithWarning()
    {
        var userId = Guid.NewGuid();
        var from = NewAccount(userId, 10m);
        var to = NewAccount(userId, 0m);
        var fx = new Fixture(from, to);

        var result = await fx.Service.CreateAsync(userId, CreateDto(from, to, 20m));

        Assert.True(result.InsufficientFundsWarning);
        Assert.Equal(-10m, from.CurrentBalance);
        Assert.Equal(20m, to.CurrentBalance);
        Assert.Single(fx.TransferRepo.Items);
    }

    // 6. Atomicidad: falla la segunda pata (SaveTransactionAsync) → todo se revierte.
    [Fact]
    public async Task CreateAsync_WhenSecondLegFails_RollsBackTransferAndBalances()
    {
        var userId = Guid.NewGuid();
        var from = NewAccount(userId, 100m);
        var to = NewAccount(userId, 0m);
        var fx = new Fixture(from, to, withRollback: true);
        fx.AccountRepo.FailOnTransactionSave = 2;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fx.Service.CreateAsync(userId, CreateDto(from, to, 20m)));

        Assert.Equal(100m, from.CurrentBalance);
        Assert.Equal(0m, to.CurrentBalance);
        Assert.Empty(fx.TransferRepo.Items);
        Assert.Empty(fx.AccountRepo.Transactions);
    }

    // 7. Idempotencia: mismo TransferGroupId dos veces no duplica ni vuelve a mover saldos.
    [Fact]
    public async Task CreateAsync_WithSameTransferGroupId_DoesNotDuplicate()
    {
        var userId = Guid.NewGuid();
        var from = NewAccount(userId, 100m);
        var to = NewAccount(userId, 0m);
        var fx = new Fixture(from, to);
        var transferGroupId = Guid.NewGuid();

        var first = await fx.Service.CreateAsync(userId, CreateDto(from, to, 20m, transferGroupId));
        var second = await fx.Service.CreateAsync(userId, CreateDto(from, to, 20m, transferGroupId));

        Assert.Equal(first.Transfer.Id, second.Transfer.Id);
        Assert.Single(fx.TransferRepo.Items);
        Assert.Equal(80m, from.CurrentBalance);
        Assert.Equal(20m, to.CurrentBalance);
    }

    // 8. Historial: GetByUserIdAsync retorna solo las transferencias del usuario correcto.
    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyTransfersForThatUser()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var from = NewAccount(userId, 100m);
        var to = NewAccount(userId, 0m);
        var fx = new Fixture(from, to);

        await fx.Service.CreateAsync(userId, CreateDto(from, to, 10m));
        await fx.Service.CreateAsync(userId, CreateDto(from, to, 5m));
        fx.TransferRepo.Items.Add(new AccountTransfer
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            FromAccount = from,
            ToAccount = to,
            Amount = 999m,
            TransferDate = fx.DateProvider.Today,
            Status = TransferStatus.Completed,
            TransferGroupId = Guid.NewGuid()
        });

        var history = await fx.Service.GetByUserIdAsync(userId);

        Assert.Equal(2, history.Count);
        Assert.All(history, t => Assert.NotEqual(999m, t.Amount));
    }

    // ── Test doubles ─────────────────────────────────────────────────────────

    private sealed class MutableBusinessDateProvider : IBusinessDateProvider
    {
        public DateOnly Today { get; set; }
        public DateOnly GetDate(DateTimeOffset instant) => Today;
    }

    private sealed class PassThroughUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) => action(cancellationToken);
    }

    private sealed class RollbackUnitOfWork(
        FakeAccountTransferRepository transferRepo, FakeAccountRepository accountRepo) : IUnitOfWork
    {
        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            var transferSnapshot = transferRepo.Items.ToList();
            var balanceSnapshot = accountRepo.Accounts.ToDictionary(a => a.Id, a => a.CurrentBalance);
            var transactionSnapshot = accountRepo.Transactions.ToList();
            try
            {
                return await action(cancellationToken);
            }
            catch
            {
                transferRepo.Items.Clear();
                transferRepo.Items.AddRange(transferSnapshot);
                foreach (var account in accountRepo.Accounts)
                    account.CurrentBalance = balanceSnapshot[account.Id];
                accountRepo.Transactions.Clear();
                accountRepo.Transactions.AddRange(transactionSnapshot);
                throw;
            }
        }
    }

    private sealed class FakeAccountTransferRepository : IAccountTransferRepository
    {
        public List<AccountTransfer> Items { get; } = new();

        public Task<AccountTransfer> CreateAsync(
            AccountTransfer entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<AccountTransfer> UpdateAsync(
            AccountTransfer entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);

        public Task<AccountTransfer?> GetOwnedByIdAsync(
            Guid id, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(t => t.Id == id && t.UserId == userId && !t.IsDeleted));

        public Task<IReadOnlyList<AccountTransfer>> GetByUserIdAsync(
            Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AccountTransfer>>(
                Items.Where(t => t.UserId == userId && !t.IsDeleted).ToList());

        public Task<AccountTransfer?> GetByTransferGroupIdAsync(
            Guid userId, Guid transferGroupId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(
                t => t.UserId == userId && t.TransferGroupId == transferGroupId && !t.IsDeleted));

        public Task<AccountTransfer?> GetByIdAsync(
            Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(t => t.Id == id));

        public Task<IEnumerable<AccountTransfer>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<AccountTransfer>>(Items);

        public Task DeleteAsync(AccountTransfer entity, CancellationToken cancellationToken = default)
        {
            Items.Remove(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAccountRepository(IEnumerable<FinancialAccount> accounts)
        : IFinancialAccountRepository
    {
        public List<FinancialAccount> Accounts { get; } = accounts.ToList();
        public List<AccountTransaction> Transactions { get; } = new();
        public int FailOnTransactionSave { get; set; }
        private int _transactionSaves;

        public Task<FinancialAccount?> GetByIdAsync(
            Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.SingleOrDefault(a => a.Id == id));

        public Task<IEnumerable<FinancialAccount>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FinancialAccount>>(Accounts);

        public Task<FinancialAccount> CreateAsync(
            FinancialAccount entity, CancellationToken cancellationToken = default)
        {
            Accounts.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<FinancialAccount> UpdateAsync(
            FinancialAccount entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);

        public Task DeleteAsync(FinancialAccount entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<FinancialAccount>> GetByUserIdAsync(
            Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinancialAccount>>(
                Accounts.Where(a => a.UserId == userId).ToList());

        public Task<FinancialAccount?> GetDefaultAsync(
            Guid userId, FinancialAccountType type, CancellationToken cancellationToken = default) =>
            Task.FromResult<FinancialAccount?>(null);

        public Task<AccountTransaction?> GetTransactionBySourceAsync(
            Guid userId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Transactions.SingleOrDefault(
                t => t.UserId == userId && t.SourceType == sourceType && t.SourceId == sourceId));

        public Task<IReadOnlyList<AccountTransaction>> GetTransactionsByTransferIdAsync(
            Guid userId, Guid transferId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AccountTransaction>>(
                Transactions.Where(t => t.UserId == userId && t.TransferId == transferId).ToList());

        public Task<IReadOnlyList<AccountTransaction>> GetRecentTransactionsAsync(
            Guid userId, int count, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AccountTransaction>>(
                Transactions.Where(t => t.UserId == userId).Take(count).ToList());

        public Task<(decimal OpeningBalances, decimal Adjustments)> GetOpeningAndAdjustmentTotalsAsync(
            Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult((
                Transactions.Where(t => t.UserId == userId && t.SourceType == "account-opening").Sum(t => t.Amount),
                Transactions.Where(t => t.UserId == userId && t.SourceType == "account-adjustment").Sum(t => t.Amount)));

        public Task SaveTransactionAsync(
            AccountTransaction transaction, CancellationToken cancellationToken = default)
        {
            _transactionSaves++;
            if (FailOnTransactionSave == _transactionSaves)
                throw new InvalidOperationException("fallo simulado");

            transaction.Id = transaction.Id == Guid.Empty ? Guid.NewGuid() : transaction.Id;
            transaction.Account = Accounts.Single(a => a.Id == transaction.AccountId);
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task DeleteTransactionAsync(
            AccountTransaction transaction, CancellationToken cancellationToken = default)
        {
            Transactions.Remove(transaction);
            return Task.CompletedTask;
        }
    }
}
