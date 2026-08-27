using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.UnitTests;

/// <summary>
/// Saneamiento del ledger (tarea 12, bloque A): resolución determinista de la
/// cuenta predeterminada, apertura registrada al crearla y conciliación atómica.
/// </summary>
public class AccountDefaultLedgerTests
{
    [Fact]
    public async Task GetDefault_WithLegacyDuplicates_AlwaysResolvesTheSameAccount()
    {
        await using var harness = await LedgerHarness.CreateAsync();

        // Datos heredados: se crearon cuando el índice único todavía no existía.
        await harness.Context.Database.ExecuteSqlRawAsync(
            "DROP INDEX ux_financial_accounts_default_per_type;");

        var oldest = harness.NewAccount("Cuenta principal", FinancialAccountType.Cash,
            isDefault: true, createdAt: new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero));
        var middle = harness.NewAccount("Cuenta principal (bis)", FinancialAccountType.Cash,
            isDefault: true, createdAt: new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero));
        var newest = harness.NewAccount("Cuenta principal (tris)", FinancialAccountType.Cash,
            isDefault: true, createdAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        harness.Context.AddRange(newest, middle, oldest);
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();

        var repository = new FinancialAccountRepository(harness.Context);
        var resolutions = new List<Guid?>();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var resolved = await repository.GetDefaultAsync(harness.UserId, FinancialAccountType.Cash);
            resolutions.Add(resolved?.Id);
            harness.Context.ChangeTracker.Clear();
        }

        var expected = await harness.Context.FinancialAccounts
            .Where(a => a.UserId == harness.UserId && a.Type == FinancialAccountType.Cash && a.IsDefault)
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .FirstAsync();

        Assert.Equal(3, new[] { oldest.Id, middle.Id, newest.Id }.Distinct().Count());
        Assert.Single(resolutions.Distinct());
        Assert.All(resolutions, id => Assert.Equal(expected, id));
    }

    [Fact]
    public async Task GetOrCreateDefault_WritesOpeningRowAndLeavesASingleDefault()
    {
        await using var harness = await LedgerHarness.CreateAsync();

        // Una predeterminada inactiva del mismo tipo: GetDefaultAsync no la devuelve,
        // pero choca con el índice único si nadie le quita la marca.
        var retired = harness.NewAccount("Portafolio viejo", FinancialAccountType.Investment,
            isDefault: true, createdAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        retired.IsActive = false;
        harness.Context.Add(retired);
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();

        // El repositorio de inversiones va en null a propósito: si el servicio volviera
        // a sembrar el saldo desde el valor de las inversiones, esto reventaría.
        var service = harness.CreateAccountService();
        var created = await service.GetOrCreateDefaultAsync(harness.UserId, FinancialAccountType.Investment);

        harness.Context.ChangeTracker.Clear();
        var accounts = await harness.Context.FinancialAccounts
            .Where(a => a.UserId == harness.UserId && a.Type == FinancialAccountType.Investment)
            .ToListAsync();
        var opening = await harness.Context.AccountTransactions
            .SingleAsync(t => t.AccountId == created.Id && t.SourceType == "account-opening");
        var ledgerBalance = await harness.Context.AccountTransactions
            .Where(t => t.AccountId == created.Id && t.DeletedAt == null)
            .SumAsync(t => t.Amount);

        Assert.Equal(0m, created.CurrentBalance);
        Assert.Equal(created.CurrentBalance, ledgerBalance);
        Assert.Equal(created.Id, Assert.Single(accounts.Where(a => a.IsDefault)).Id);
        Assert.False(accounts.Single(a => a.Id == retired.Id).IsDefault);
        Assert.Equal(0m, opening.Amount);
        Assert.Equal(created.Id, opening.SourceId);
    }

    [Fact]
    public async Task Update_RejectsSilentBalanceChange()
    {
        await using var harness = await LedgerHarness.CreateAsync();
        var account = harness.NewAccount("Efectivo", FinancialAccountType.Cash, isDefault: true);
        account.CurrentBalance = 100m;
        harness.Context.Add(account);
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();

        var service = harness.CreateAccountService();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => service.UpdateAsync(account.Id, harness.UserId, new FinancialAccountUpdateDto
            {
                Name = "Efectivo",
                CurrentBalance = 250m,
                IsDefault = true,
                IsActive = true
            }));

        harness.Context.ChangeTracker.Clear();
        var persisted = await harness.Context.FinancialAccounts.SingleAsync(a => a.Id == account.Id);

        Assert.Equal("BALANCE_CHANGE_REQUIRES_RECONCILIATION", error.Code);
        Assert.Equal(100m, persisted.CurrentBalance);
        Assert.Empty(await harness.Context.AccountTransactions
            .Where(t => t.SourceType == "account-adjustment").ToListAsync());
    }

    [Fact]
    public async Task Reconciliation_WhenPersistingTheReconciliationFails_RollsBackTheAdjustment()
    {
        await using var harness = await LedgerHarness.CreateAsync();
        var account = await harness.SeedAccountWithOpeningAsync(100m);

        var service = new AccountReconciliationService(
            new FailingReconciliationRepository(new AccountReconciliationRepository(harness.Context)),
            new FinancialAccountRepository(harness.Context),
            new EcuadorBusinessDateProvider(TimeProvider.System),
            new UnitOfWork(harness.Context));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApplyAsync(account.Id, harness.UserId, new ReconciliationCreateDto(
                ActualBalance: 140m,
                ReconciliationDate: new DateOnly(2026, 8, 20),
                Notes: "Prueba de rollback")));

        harness.Context.ChangeTracker.Clear();
        var persisted = await harness.Context.FinancialAccounts.SingleAsync(a => a.Id == account.Id);
        var adjustments = await harness.Context.AccountTransactions
            .Where(t => t.SourceType == "account-adjustment").ToListAsync();
        var reconciliations = await harness.Context.AccountReconciliations.ToListAsync();

        Assert.Equal(100m, persisted.CurrentBalance);
        Assert.Empty(adjustments);
        Assert.Empty(reconciliations);
    }

    [Fact]
    public async Task Reconciliation_WhenEverythingSucceeds_PersistsAdjustmentAndReconciliation()
    {
        await using var harness = await LedgerHarness.CreateAsync();
        var account = await harness.SeedAccountWithOpeningAsync(100m);

        var service = new AccountReconciliationService(
            new AccountReconciliationRepository(harness.Context),
            new FinancialAccountRepository(harness.Context),
            new EcuadorBusinessDateProvider(TimeProvider.System),
            new UnitOfWork(harness.Context));

        var result = await service.ApplyAsync(account.Id, harness.UserId, new ReconciliationCreateDto(
            ActualBalance: 140m,
            ReconciliationDate: new DateOnly(2026, 8, 20),
            Notes: null));

        harness.Context.ChangeTracker.Clear();
        var persisted = await harness.Context.FinancialAccounts.SingleAsync(a => a.Id == account.Id);
        var adjustment = await harness.Context.AccountTransactions
            .SingleAsync(t => t.SourceType == "account-adjustment");

        Assert.Equal(40m, result.Difference);
        Assert.Equal(140m, persisted.CurrentBalance);
        Assert.Equal(40m, adjustment.Amount);
        Assert.Equal(adjustment.Id, result.AdjustmentTransactionId);
    }

    private sealed class LedgerHarness : IAsyncDisposable
    {
        private SqliteConnection _connection = null!;
        public AppDbContext Context { get; private set; } = null!;
        public Guid UserId { get; private set; }

        public static async Task<LedgerHarness> CreateAsync()
        {
            var harness = new LedgerHarness();
            harness._connection = new SqliteConnection("Data Source=:memory:");
            await harness._connection.OpenAsync();
            harness._connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());
            harness._connection.CreateFunction<string>("NOW", () => DateTimeOffset.UtcNow.ToString("O"));

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(harness._connection).Options;
            harness.Context = new AppDbContext(options);
            await harness.Context.Database.EnsureCreatedAsync();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"ledger-{Guid.NewGuid():N}@example.test",
                PasswordHash = "hash",
                FirstName = "Ledger",
                LastName = "Test"
            };
            harness.UserId = user.Id;
            harness.Context.Add(user);
            await harness.Context.SaveChangesAsync();
            harness.Context.ChangeTracker.Clear();
            return harness;
        }

        public FinancialAccount NewAccount(
            string name, FinancialAccountType type, bool isDefault,
            DateTimeOffset? createdAt = null)
        {
            var stamp = createdAt ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            return new FinancialAccount
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                Name = name,
                Type = type,
                CurrentBalance = 0m,
                IsDefault = isDefault,
                IsActive = true,
                CreatedAt = stamp,
                UpdatedAt = stamp
            };
        }

        public async Task<FinancialAccount> SeedAccountWithOpeningAsync(decimal openingBalance)
        {
            var account = NewAccount("Efectivo", FinancialAccountType.Cash, isDefault: true);
            account.CurrentBalance = openingBalance;
            Context.Add(account);
            Context.Add(new AccountTransaction
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                AccountId = account.Id,
                Amount = openingBalance,
                Date = new DateOnly(2026, 8, 1),
                Description = "Apertura de cuenta (no es un ingreso)",
                SourceType = "account-opening",
                SourceId = account.Id
            });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return account;
        }

        public FinancialAccountService CreateAccountService() =>
            new(new FinancialAccountRepository(Context), null!, null!, new UnitOfWork(Context));

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    /// <summary>
    /// Deja pasar todo menos el guardado de la conciliación, que falla justo después
    /// de que el ajuste y el nuevo saldo ya se escribieron.
    /// </summary>
    private sealed class FailingReconciliationRepository(IAccountReconciliationRepository inner)
        : IAccountReconciliationRepository
    {
        public Task<AccountReconciliation> CreateAsync(
            AccountReconciliation entity, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Fallo simulado al guardar la conciliación.");

        public Task<AccountReconciliation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            inner.GetByIdAsync(id, cancellationToken);

        public Task<IEnumerable<AccountReconciliation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            inner.GetAllAsync(cancellationToken);

        public Task<AccountReconciliation> UpdateAsync(
            AccountReconciliation entity, CancellationToken cancellationToken = default) =>
            inner.UpdateAsync(entity, cancellationToken);

        public Task DeleteAsync(AccountReconciliation entity, CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(entity, cancellationToken);

        public Task<IReadOnlyList<AccountReconciliation>> GetByAccountAsync(
            Guid accountId, Guid userId, int page, int pageSize,
            CancellationToken cancellationToken = default) =>
            inner.GetByAccountAsync(accountId, userId, page, pageSize, cancellationToken);

        public Task<AccountReconciliation?> GetLastByAccountAsync(
            Guid accountId, Guid userId, CancellationToken cancellationToken = default) =>
            inner.GetLastByAccountAsync(accountId, userId, cancellationToken);

        public Task<decimal> GetLedgerBalanceAsync(
            Guid accountId, CancellationToken cancellationToken = default) =>
            inner.GetLedgerBalanceAsync(accountId, cancellationToken);
    }
}
