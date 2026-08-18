using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.UnitTests;

public class FinancialPositionServiceIntegrationTests
{
    [Fact]
    public async Task GetAsync_IsolatesEveryBalanceByAuthenticatedUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = await CreateContextAsync(connection);

        var owner = User("owner@position.test");
        var other = User("other@position.test");
        context.AddRange(owner, other);
        context.FinancialAccounts.AddRange(
            Account(owner.Id, 100m),
            Account(other.Id, 999m));
        context.CreditCards.AddRange(
            Card(owner.Id, 20m),
            Card(other.Id, -500m));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetAsync(owner.Id);

        Assert.Equal(100m, result.Assets.CashAccounts);
        Assert.Equal(20m, result.Liabilities.CreditCards);
        Assert.Equal(0m, result.Assets.CreditCardCredits);
        Assert.Equal(80m, result.NetWorth);
    }

    [Fact]
    public async Task GetAsync_RejectsHistoricalCutoffInsteadOfInventingBalances()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = await CreateContextAsync(connection);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetAsync(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today).AddDays(-1)));

        Assert.Equal("HISTORICAL_FINANCIAL_POSITION_NOT_AVAILABLE", exception.Code);
    }

    private static async Task<AppDbContext> CreateContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static FinancialPositionService CreateService(AppDbContext context) => new(
        new FinancialAccountRepository(context),
        new InvestmentRepository(context),
        new EmptyDebtRepository(),
        new CreditCardRepository(context),
        new SavingsGoalRepository(context));

    private static User User(string email) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        PasswordHash = "x",
        FirstName = "Position",
        LastName = "Test"
    };

    private static FinancialAccount Account(Guid userId, decimal balance) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = "Cuenta",
        Type = FinancialAccountType.Cash,
        CurrentBalance = balance
    };

    private static CreditCard Card(Guid userId, decimal balance) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = "Tarjeta",
        CurrentBalance = balance,
        ClosingDay = 10,
        DueDay = 25
    };
}
