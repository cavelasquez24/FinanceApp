using FinanceApp.Application.DTOs.SavingsGoal;
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
        context.AddRange(user, cash);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var savingsRepository = new SavingsGoalRepository(context);
        var service = new SavingsGoalService(savingsRepository, unitOfWork);

        var first = await service.CreateAsync(user.Id, CreateGoal("Vacaciones", 500m, 300m));
        var second = await service.CreateAsync(user.Id, CreateGoal("Computadora", 400m, 100m));

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
        context.AddRange(user, cash);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);
        var savingsRepository = new SavingsGoalRepository(context);
        var service = new SavingsGoalService(savingsRepository, unitOfWork);
        var goal = await service.CreateAsync(user.Id, CreateGoal("Reserva", 300m, 100m));

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
        context.AddRange(firstUser, secondUser);
        await context.SaveChangesAsync();

        var repository = new SavingsGoalRepository(context);
        var service = new SavingsGoalService(repository, new UnitOfWork(context));

        await service.CreateAsync(firstUser.Id, new SavingsGoalCreateDto { Name = "Reserva A", TargetAmount = 500m, Purpose = "emergency_fund", MinimumProtectedAmount = 100m });
        await service.CreateAsync(secondUser.Id, new SavingsGoalCreateDto { Name = "Reserva B", TargetAmount = 500m, Purpose = "emergency_fund", MinimumProtectedAmount = 100m });

        var exception = await Assert.ThrowsAsync<FinanceApp.Domain.Exceptions.DomainException>(() =>
            service.CreateAsync(firstUser.Id, new SavingsGoalCreateDto { Name = "Duplicado", TargetAmount = 100m, Purpose = "emergency_fund", MinimumProtectedAmount = 10m }));

        Assert.Equal("EMERGENCY_FUND_ALREADY_EXISTS", exception.Code);
        Assert.Equal(2, await context.SavingsGoals.CountAsync());
    }
    private static SavingsGoalCreateDto CreateGoal(string name, decimal target, decimal initial) => new()
    {
        Name = name,
        TargetAmount = target,
        InitialAmount = initial,
        InitialFundingDate = new DateOnly(2026, 8, 17),
        IdempotencyKey = Guid.NewGuid(),
        Purpose = "general"
    };
}