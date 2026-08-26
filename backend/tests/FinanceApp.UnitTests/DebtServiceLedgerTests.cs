using FinanceApp.Application.DTOs.Debt;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.UnitTests;

/// <summary>
/// Trazabilidad del ledger en deudas. Antes existía una rama condicional
/// (Debt.LinkedSavingsGoalId) que, cuando la deuda estaba ligada a una meta de
/// ahorro, se saltaba SyncMovementAsync y dejaba el movimiento huérfano.
/// Retirada esa rama, pago y desembolso siempre registran su movimiento.
/// </summary>
public class DebtServiceLedgerTests
{
    [Fact]
    public async Task AddPaymentAsync_AlwaysRecordsLedgerMovement()
    {
        await using var harness = await DebtHarness.CreateAsync();

        await harness.Service.AddPaymentAsync(harness.DebtId, harness.UserId, new DebtPaymentCreateDto
        {
            PaymentDate = new DateOnly(2026, 8, 17),
            Amount = 150m,
            PrincipalAmount = 120m,
            InterestAmount = 30m
        });

        var movement = Assert.Single(harness.AccountService.Movements);
        Assert.Equal("debt-payment", movement.SourceType);
        Assert.Equal(-150m, movement.SignedAmount);
        Assert.Equal(new DateOnly(2026, 8, 17), movement.Date);

        harness.Context.ChangeTracker.Clear();
        var debt = await harness.Context.Debts.SingleAsync(d => d.Id == harness.DebtId);
        Assert.Equal(DebtHarness.InitialBalance - 120m, debt.CurrentBalance);

        // El movimiento apunta al DebtPayment persistido: sin huérfanos.
        var payment = await harness.Context.DebtPayments.SingleAsync();
        Assert.Equal(payment.Id, movement.SourceId);
    }

    [Fact]
    public async Task AddWithdrawalAsync_AlwaysRecordsLedgerMovement()
    {
        await using var harness = await DebtHarness.CreateAsync();

        await harness.Service.AddWithdrawalAsync(harness.DebtId, harness.UserId, new DebtWithdrawalCreateDto
        {
            WithdrawalDate = new DateOnly(2026, 8, 17),
            Amount = 300m
        });

        var movement = Assert.Single(harness.AccountService.Movements);
        Assert.Equal("debt-withdrawal", movement.SourceType);
        Assert.Equal(300m, movement.SignedAmount);

        harness.Context.ChangeTracker.Clear();
        var debt = await harness.Context.Debts.SingleAsync(d => d.Id == harness.DebtId);
        Assert.Equal(DebtHarness.InitialBalance + 300m, debt.CurrentBalance);
        Assert.Equal(DebtHarness.InitialAmount + 300m, debt.OriginalAmount);

        var withdrawal = await harness.Context.DebtWithdrawals.SingleAsync();
        Assert.Equal(withdrawal.Id, movement.SourceId);
    }

    private sealed class DebtHarness : IAsyncDisposable
    {
        public const decimal InitialAmount = 2_000m;
        public const decimal InitialBalance = 1_800m;

        private readonly SqliteConnection _connection;

        private DebtHarness(SqliteConnection connection, AppDbContext context,
            DebtService service, RecordingAccountService accountService,
            Guid userId, Guid debtId)
        {
            _connection = connection;
            Context = context;
            Service = service;
            AccountService = accountService;
            UserId = userId;
            DebtId = debtId;
        }

        public AppDbContext Context { get; }
        public DebtService Service { get; }
        public RecordingAccountService AccountService { get; }
        public Guid UserId { get; }
        public Guid DebtId { get; }

        public static async Task<DebtHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "debt-ledger@example.test",
                PasswordHash = "hash",
                FirstName = "Debt",
                LastName = "Test"
            };
            var cash = new FinancialAccount
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = "Cuenta principal",
                Type = FinancialAccountType.Cash,
                CurrentBalance = 5_000m,
                IsDefault = true,
                IsActive = true
            };
            var debt = new Debt
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = "Préstamo bancario",
                Type = DebtType.Loan,
                OriginalAmount = InitialAmount,
                CurrentBalance = InitialBalance,
                StartDate = new DateOnly(2026, 1, 1),
                IsActive = true
            };
            context.AddRange(user, cash, debt);
            await context.SaveChangesAsync();

            var accountService = new RecordingAccountService(cash.Id, cash.Id, 5_000m);
            var service = new DebtService(new DebtRepository(context), accountService);

            return new DebtHarness(connection, context, service, accountService, user.Id, debt.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
