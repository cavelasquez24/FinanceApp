using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.UnitTests;

public class EmergencyFundInternalRestorationTests
{
    [Fact]
    public async Task UseAndRestoration_UpdateOnlyGoalData()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new User { Id = Guid.NewGuid(), Email = "internal-restoration@example.test", PasswordHash = "hash", FirstName = "Internal", LastName = "Test" };
        var cash = new FinancialAccount { Id = Guid.NewGuid(), UserId = user.Id, Name = "Cuenta principal", Type = FinancialAccountType.Cash, CurrentBalance = 1_500m, IsDefault = true, IsActive = true };
        var savings = new FinancialAccount { Id = Guid.NewGuid(), UserId = user.Id, Name = "Cuenta de ahorro", Type = FinancialAccountType.Savings, CurrentBalance = 1_000m, IsDefault = false, IsActive = true };
        var goal = new SavingsGoal { Id = Guid.NewGuid(), UserId = user.Id, Name = "Fondo", Purpose = SavingsGoalPurpose.EmergencyFund, TargetAmount = 1_000m, CurrentAmount = 1_000m, MinimumProtectedAmount = 500m, SavingsAccountId = savings.Id };
        context.AddRange(user, cash, savings, goal);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        // Stub accountService: returns real account IDs but sync operations are no-ops.
        // This isolates the test to goal-level data changes without writing real account transactions.
        var accountService = new FakeAccountServiceForEF(savings.Id, cash.Id);
        var service = new EmergencyFundRestorationService(
            new SavingsGoalRepository(context),
            new EmergencyFundRestorationRepository(context),
            null!,
            accountService,
            unitOfWork);

        var use = await service.CreateUseAsync(goal.Id, user.Id, new EmergencyFundUseCreateDto
        {
            FundedAmount = 300m,
            Description = "Emergencia médica",
            UseMode = "account_transfer",
            DestinationAccountId = cash.Id,
            AcquisitionDate = new DateOnly(2026, 8, 17),
            FirstScheduledDate = new DateOnly(2026, 9, 17),
            TargetRestorationDate = new DateOnly(2026, 11, 17),
            ScheduledContributionAmount = 100m,
            IdempotencyKey = Guid.NewGuid()
        });
        await service.RegisterPaymentAsync(use.Id, user.Id, new EmergencyFundRestorationPaymentDto
        {
            Amount = 100m,
            PaymentDate = new DateOnly(2026, 8, 17),
            FundingMode = "existing_balance",
            IdempotencyKey = Guid.NewGuid()
        });

        context.ChangeTracker.Clear();
        Assert.Equal(1_500m, (await context.FinancialAccounts.SingleAsync(a => a.Id == cash.Id)).CurrentBalance);
        Assert.Equal(800m, (await context.SavingsGoals.SingleAsync(item => item.Id == goal.Id)).CurrentAmount);
        Assert.Empty(await context.AccountTransactions.ToListAsync());
        Assert.Empty(await context.Expenses.ToListAsync());
        var restoration = await context.EmergencyFundRestorations.SingleAsync();
        Assert.Null(restoration.LinkedExpenseId);
        Assert.Equal(200m, restoration.OutstandingAmount);
    }

    private sealed class FakeAccountServiceForEF : IFinancialAccountService
    {
        private readonly FinancialAccountResponseDto _savings;
        private readonly FinancialAccountResponseDto _cash;

        public FakeAccountServiceForEF(Guid savingsId, Guid cashId)
        {
            _savings = new FinancialAccountResponseDto { Id = savingsId, Name = "Ahorro (stub EF)", Type = "savings", IsActive = true, IsDefault = false, CurrentBalance = 1_000m };
            _cash = new FinancialAccountResponseDto { Id = cashId, Name = "Cash (stub)", Type = "cash", IsActive = true };
        }

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
        public Task SyncMovementAsync(Guid userId, Guid? accountId, FinancialAccountType fallbackType, decimal signedAmount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SyncTransferAsync(Guid userId, FinancialAccountType fromType, FinancialAccountType toType, decimal amount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SyncTransferBetweenAccountsAsync(Guid userId, Guid? fromAccountId, FinancialAccountType fromFallbackType, Guid? toAccountId, FinancialAccountType toFallbackType, decimal amount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
