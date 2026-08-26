using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.UnitTests;

/// <summary>
/// El préstamo interno (retiro con compromiso de devolución) es exclusivo del
/// fondo de emergencia. Una meta general se gasta en su propósito o se
/// reasigna, nunca se presta.
/// </summary>
public class SavingsGoalTemporaryLoanTests
{
    [Fact]
    public async Task WithdrawAsync_TemporaryLoan_OnNonEmergencyGoal_Throws()
    {
        await using var harness = await LoanHarness.CreateAsync(SavingsGoalPurpose.General);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            harness.Service.WithdrawAsync(harness.GoalId, harness.UserId, new SavingsGoalWithdrawalCreateDto
            {
                Amount = 200m,
                WithdrawalDate = new DateOnly(2026, 8, 17),
                Reason = SavingsWithdrawalReason.TemporaryLoan,
                DestinationAccountId = harness.CashAccountId,
                IdempotencyKey = Guid.NewGuid()
            }));

        Assert.Equal("LOAN_ONLY_FROM_EMERGENCY_FUND", exception.Code);

        // El estado debe quedar exactamente como antes del intento.
        harness.Context.ChangeTracker.Clear();
        var goal = await harness.Context.SavingsGoals.SingleAsync(g => g.Id == harness.GoalId);
        Assert.Equal(LoanHarness.InitialGoalAmount, goal.CurrentAmount);
        Assert.Empty(await harness.Context.SavingsGoalWithdrawals.ToListAsync());
        Assert.Empty(await harness.Context.AccountTransactions.ToListAsync());
        Assert.Empty(harness.AccountService.Transfers);
        Assert.Empty(harness.AccountService.Movements);
    }

    [Fact]
    public async Task WithdrawAsync_TemporaryLoan_OnEmergencyFundGoal_Succeeds()
    {
        await using var harness = await LoanHarness.CreateAsync(SavingsGoalPurpose.EmergencyFund);

        var result = await harness.Service.WithdrawAsync(harness.GoalId, harness.UserId, new SavingsGoalWithdrawalCreateDto
        {
            Amount = 200m,
            WithdrawalDate = new DateOnly(2026, 8, 17),
            Reason = SavingsWithdrawalReason.TemporaryLoan,
            DestinationAccountId = harness.CashAccountId,
            IdempotencyKey = Guid.NewGuid()
        });

        Assert.Equal(200m, result.Amount);
        Assert.Equal(SavingsWithdrawalReason.TemporaryLoan, result.Reason);

        harness.Context.ChangeTracker.Clear();
        var goal = await harness.Context.SavingsGoals.SingleAsync(g => g.Id == harness.GoalId);
        Assert.Equal(LoanHarness.InitialGoalAmount - 200m, goal.CurrentAmount);

        var withdrawal = await harness.Context.SavingsGoalWithdrawals.SingleAsync();
        Assert.Equal(SavingsWithdrawalReason.TemporaryLoan, withdrawal.Reason);
        Assert.Equal(harness.CashAccountId, withdrawal.DestinationAccountId);

        // El dinero sale del ahorro hacia la cuenta líquida: neutro en patrimonio.
        var transfer = Assert.Single(harness.AccountService.Transfers);
        Assert.Equal(harness.SavingsAccountId, transfer.FromAccountId);
        Assert.Equal(harness.CashAccountId, transfer.ToAccountId);
        Assert.Equal(200m, transfer.Amount);
    }

    private sealed class LoanHarness : IAsyncDisposable
    {
        public const decimal InitialGoalAmount = 1_000m;

        private readonly SqliteConnection _connection;

        private LoanHarness(SqliteConnection connection, AppDbContext context,
            SavingsGoalService service, RecordingAccountService accountService,
            Guid userId, Guid goalId, Guid savingsAccountId, Guid cashAccountId)
        {
            _connection = connection;
            Context = context;
            Service = service;
            AccountService = accountService;
            UserId = userId;
            GoalId = goalId;
            SavingsAccountId = savingsAccountId;
            CashAccountId = cashAccountId;
        }

        public AppDbContext Context { get; }
        public SavingsGoalService Service { get; }
        public RecordingAccountService AccountService { get; }
        public Guid UserId { get; }
        public Guid GoalId { get; }
        public Guid SavingsAccountId { get; }
        public Guid CashAccountId { get; }

        public static async Task<LoanHarness> CreateAsync(SavingsGoalPurpose purpose)
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
                Email = "temporary-loan@example.test",
                PasswordHash = "hash",
                FirstName = "Loan",
                LastName = "Test"
            };
            var cash = new FinancialAccount
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = "Cuenta principal",
                Type = FinancialAccountType.Cash,
                CurrentBalance = 500m,
                IsDefault = true,
                IsActive = true
            };
            var savings = new FinancialAccount
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = "Cuenta de ahorro",
                Type = FinancialAccountType.Savings,
                CurrentBalance = InitialGoalAmount,
                IsDefault = false,
                IsActive = true
            };
            var goal = new SavingsGoal
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = purpose == SavingsGoalPurpose.EmergencyFund ? "Fondo de emergencia" : "Vacaciones",
                Purpose = purpose,
                TargetAmount = InitialGoalAmount,
                CurrentAmount = InitialGoalAmount,
                MinimumProtectedAmount = purpose == SavingsGoalPurpose.EmergencyFund ? 500m : null,
                SavingsAccountId = savings.Id
            };
            context.AddRange(user, cash, savings, goal);
            await context.SaveChangesAsync();

            var accountService = new RecordingAccountService(savings.Id, cash.Id, InitialGoalAmount);
            var service = new SavingsGoalService(
                new SavingsGoalRepository(context), accountService, null!, new UnitOfWork(context));

            return new LoanHarness(connection, context, service, accountService,
                user.Id, goal.Id, savings.Id, cash.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}

/// <summary>
/// Stub de IFinancialAccountService que registra los movimientos y
/// transferencias solicitados, para poder afirmar que el ledger se tocó (o
/// no se tocó) sin escribir AccountTransactions reales.
/// </summary>
internal sealed class RecordingAccountService : IFinancialAccountService
{
    private readonly FinancialAccountResponseDto _savings;
    private readonly FinancialAccountResponseDto _cash;

    public RecordingAccountService(Guid savingsId, Guid cashId, decimal savingsBalance)
    {
        _savings = new FinancialAccountResponseDto
        {
            Id = savingsId,
            Name = "Ahorro (stub)",
            Type = "savings",
            IsActive = true,
            IsDefault = false,
            CurrentBalance = savingsBalance
        };
        _cash = new FinancialAccountResponseDto
        {
            Id = cashId,
            Name = "Cash (stub)",
            Type = "cash",
            IsActive = true,
            IsDefault = true,
            CurrentBalance = 500m
        };
    }

    public List<MovementRecord> Movements { get; } = [];
    public List<TransferRecord> Transfers { get; } = [];

    public Task<IReadOnlyList<FinancialAccountResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FinancialAccountResponseDto>>([_savings, _cash]);
    public Task<FinancialAccountResponseDto> GetOrCreateDefaultAsync(Guid userId, FinancialAccountType type, CancellationToken cancellationToken = default) =>
        Task.FromResult(type == FinancialAccountType.Savings ? _savings : _cash);
    public Task<IReadOnlyList<AccountTransactionResponseDto>> GetRecentTransactionsAsync(Guid userId, int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AccountTransactionResponseDto>>([]);
    public Task<FinancialAccountResponseDto> CreateAsync(Guid userId, FinancialAccountCreateDto dto, CancellationToken cancellationToken = default) => Task.FromResult(_savings);
    public Task<FinancialAccountResponseDto> UpdateAsync(Guid id, Guid userId, FinancialAccountUpdateDto dto, CancellationToken cancellationToken = default) => Task.FromResult(_savings);
    public Task<AccountTransferResponseDto> TransferAsync(Guid userId, AccountTransferCreateDto dto, CancellationToken cancellationToken = default) => Task.FromResult(new AccountTransferResponseDto());
    public Task<decimal> GetAvailableBalanceAsync(Guid userId, Guid? accountId, FinancialAccountType fallbackType, CancellationToken cancellationToken = default) => Task.FromResult(10_000m);

    public Task SyncMovementAsync(Guid userId, Guid? accountId, FinancialAccountType fallbackType, decimal signedAmount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default)
    {
        Movements.Add(new MovementRecord(accountId, signedAmount, date, sourceType, sourceId, description));
        return Task.CompletedTask;
    }

    public Task SyncTransferAsync(Guid userId, FinancialAccountType fromType, FinancialAccountType toType, decimal amount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SyncTransferBetweenAccountsAsync(Guid userId, Guid? fromAccountId, FinancialAccountType fromFallbackType, Guid? toAccountId, FinancialAccountType toFallbackType, decimal amount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default)
    {
        Transfers.Add(new TransferRecord(fromAccountId, toAccountId, amount, date, sourceType, sourceId));
        return Task.CompletedTask;
    }

    internal sealed record MovementRecord(
        Guid? AccountId, decimal SignedAmount, DateOnly Date,
        string SourceType, Guid SourceId, string Description);

    internal sealed record TransferRecord(
        Guid? FromAccountId, Guid? ToAccountId, decimal Amount,
        DateOnly Date, string SourceType, Guid SourceId);
}
