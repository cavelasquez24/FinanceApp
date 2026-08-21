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

public class SavingsGoalAllocationIntegrationTests
{
    [Fact]
    public async Task CreatingMultipleFundedGoals_AllocatesWithoutChangingPhysicalBalances()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "goals@example.test",
            PasswordHash = "hash",
            FirstName = "Goals",
            LastName = "Test"
        };
        var cash = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Cuenta principal",
            Type = FinancialAccountType.Cash,
            CurrentBalance = 1_000m,
            IsDefault = true,
            IsActive = true
        };
        var savings = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Cuenta de ahorro",
            Type = FinancialAccountType.Savings,
            CurrentBalance = 10_000m,
            IsDefault = false,
            IsActive = true
        };
        context.AddRange(user, cash, savings);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var savingsRepository = new SavingsGoalRepository(context);
        var service = new SavingsGoalService(savingsRepository, new FakeAccountService(savings.Id), null!, unitOfWork);

        var first = await service.CreateAsync(user.Id, CreateGoal("Vacaciones", 500m, 300m, savings.Id));
        var second = await service.CreateAsync(user.Id, CreateGoal("Computadora", 400m, 100m, savings.Id));

        context.ChangeTracker.Clear();
        var persistedCash = await context.FinancialAccounts.SingleAsync(account => account.Id == cash.Id);
        var goals = await context.SavingsGoals.Where(goal => goal.UserId == user.Id && goal.DeletedAt == null).ToListAsync();
        var accountMovements = await context.AccountTransactions
            .Where(transaction => transaction.UserId == user.Id)
            .ToListAsync();

        Assert.Equal(1_000m, persistedCash.CurrentBalance);
        Assert.Equal(2, goals.Count);
        Assert.Equal(400m, goals.Sum(goal => goal.CurrentAmount));
        Assert.Empty(accountMovements);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task ReleasingGoalAllocation_DoesNotCreateAnAccountTransfer()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new User { Id = Guid.NewGuid(), Email = "release@example.test", PasswordHash = "hash", FirstName = "Release", LastName = "Test" };
        var cash = new FinancialAccount { Id = Guid.NewGuid(), UserId = user.Id, Name = "Cuenta principal", Type = FinancialAccountType.Cash, CurrentBalance = 500m, IsDefault = true, IsActive = true };
        var savings = new FinancialAccount { Id = Guid.NewGuid(), UserId = user.Id, Name = "Cuenta de ahorro", Type = FinancialAccountType.Savings, CurrentBalance = 10_000m, IsDefault = false, IsActive = true };
        context.AddRange(user, cash, savings);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var savingsRepository = new SavingsGoalRepository(context);
        var service = new SavingsGoalService(savingsRepository, new FakeAccountService(savings.Id), null!, unitOfWork);
        var goal = await service.CreateAsync(user.Id, CreateGoal("Reserva", 300m, 100m, savings.Id));

        await service.WithdrawAsync(goal.Id, user.Id, new SavingsGoalWithdrawalCreateDto
        {
            Amount = 40m,
            WithdrawalDate = new DateOnly(2026, 8, 17),
            Reason = SavingsWithdrawalReason.ReallocatedToLiquid,
            IdempotencyKey = Guid.NewGuid()
        });

        context.ChangeTracker.Clear();
        Assert.Equal(500m, (await context.FinancialAccounts.SingleAsync(account => account.Id == cash.Id)).CurrentBalance);
        Assert.Equal(60m, (await context.SavingsGoals.SingleAsync(item => item.Id == goal.Id)).CurrentAmount);
        Assert.Empty(await context.AccountTransactions.Where(transaction => transaction.UserId == user.Id).ToListAsync());
        Assert.Single(await context.SavingsGoalWithdrawals.Where(item => item.SavingsGoalId == goal.Id).ToListAsync());
    }

    [Fact]
    public async Task EmergencyFund_IsUniquePerUser_NotGlobally()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var firstUser = new User { Id = Guid.NewGuid(), Email = "first-emergency@example.test", PasswordHash = "hash", FirstName = "First", LastName = "User" };
        var secondUser = new User { Id = Guid.NewGuid(), Email = "second-emergency@example.test", PasswordHash = "hash", FirstName = "Second", LastName = "User" };
        var savingsA = new FinancialAccount { Id = Guid.NewGuid(), UserId = firstUser.Id, Name = "Ahorro A", Type = FinancialAccountType.Savings, CurrentBalance = 5_000m, IsDefault = true, IsActive = true };
        var savingsB = new FinancialAccount { Id = Guid.NewGuid(), UserId = secondUser.Id, Name = "Ahorro B", Type = FinancialAccountType.Savings, CurrentBalance = 5_000m, IsDefault = true, IsActive = true };
        context.AddRange(firstUser, secondUser, savingsA, savingsB);
        await context.SaveChangesAsync();

        var repository = new SavingsGoalRepository(context);
        // FakeAccountService returns the stub savings account; each user shares the same stub but the service uses GetAllAsync to validate
        var fakeSvcA = new FakeAccountService(savingsA.Id);
        var fakeSvcB = new FakeAccountService(savingsB.Id);
        var serviceA = new SavingsGoalService(repository, fakeSvcA, null!, new UnitOfWork(context));
        var serviceB = new SavingsGoalService(repository, fakeSvcB, null!, new UnitOfWork(context));
        var serviceA2 = new SavingsGoalService(repository, fakeSvcA, null!, new UnitOfWork(context));

        await serviceA.CreateAsync(firstUser.Id, new SavingsGoalCreateDto { Name = "Reserva A", TargetAmount = 500m, Purpose = "emergency_fund", MinimumProtectedAmount = 100m, SavingsAccountId = savingsA.Id });
        await serviceB.CreateAsync(secondUser.Id, new SavingsGoalCreateDto { Name = "Reserva B", TargetAmount = 500m, Purpose = "emergency_fund", MinimumProtectedAmount = 100m, SavingsAccountId = savingsB.Id });

        var exception = await Assert.ThrowsAsync<FinanceApp.Domain.Exceptions.DomainException>(() =>
            serviceA2.CreateAsync(firstUser.Id, new SavingsGoalCreateDto { Name = "Duplicado", TargetAmount = 100m, Purpose = "emergency_fund", MinimumProtectedAmount = 10m, SavingsAccountId = savingsA.Id }));

        Assert.Equal("EMERGENCY_FUND_ALREADY_EXISTS", exception.Code);
        Assert.Equal(2, await context.SavingsGoals.CountAsync());
    }
    private static SavingsGoalCreateDto CreateGoal(string name, decimal target, decimal initial, Guid savingsAccountId) => new()
    {
        Name = name,
        TargetAmount = target,
        InitialAmount = initial,
        InitialFundingDate = new DateOnly(2026, 8, 17),
        InitialFundingMode = "existing_balance",
        IdempotencyKey = Guid.NewGuid(),
        Purpose = "general",
        SavingsAccountId = savingsAccountId
    };

    private sealed class FakeAccountService : IFinancialAccountService
    {
        private readonly FinancialAccountResponseDto _savings;

        public FakeAccountService(Guid savingsId)
        {
            _savings = new FinancialAccountResponseDto
            {
                Id = savingsId,
                Name = "Ahorro (stub)",
                Type = "savings",
                IsActive = true,
                IsDefault = true,
                CurrentBalance = 10_000m
            };
        }

        public Task<IReadOnlyList<FinancialAccountResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinancialAccountResponseDto>>([_savings]);
        public Task<FinancialAccountResponseDto> GetOrCreateDefaultAsync(Guid userId, FinancialAccountType type, CancellationToken cancellationToken = default) =>
            Task.FromResult(_savings);
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