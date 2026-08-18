using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.UnitTests;

public class FinancialAccountTransferIntegrationTests
{
    [Fact]
    public async Task Transfer_PersistsBalancedLegsAndBalancesInOneDatabaseTransaction()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new User { Id = Guid.NewGuid(), Email = "transfer@example.test", PasswordHash = "hash", FirstName = "Transfer", LastName = "Test" };
        var cash = new FinancialAccount { Id = Guid.NewGuid(), UserId = user.Id, Name = "Efectivo", Type = FinancialAccountType.Cash, CurrentBalance = 100m, IsActive = true };
        var operating = new FinancialAccount { Id = Guid.NewGuid(), UserId = user.Id, Name = "Operativa", Type = FinancialAccountType.Cash, CurrentBalance = 0m, IsActive = true };
        context.AddRange(user, cash, operating);
        await context.SaveChangesAsync();

        var service = new FinancialAccountService(
            new FinancialAccountRepository(context), null!, null!, new UnitOfWork(context));
        var transferId = Guid.NewGuid();

        await service.TransferAsync(user.Id, new AccountTransferCreateDto
        {
            FromAccountId = cash.Id,
            ToAccountId = operating.Id,
            Amount = 20m,
            Date = new DateOnly(2026, 8, 16),
            IdempotencyKey = transferId
        });

        context.ChangeTracker.Clear();
        var persistedCash = await context.FinancialAccounts.SingleAsync(account => account.Id == cash.Id);
        var persistedOperating = await context.FinancialAccounts.SingleAsync(account => account.Id == operating.Id);
        var movements = await context.AccountTransactions.Where(transaction => transaction.TransferId == transferId).ToListAsync();

        Assert.Equal(80m, persistedCash.CurrentBalance);
        Assert.Equal(20m, persistedOperating.CurrentBalance);
        Assert.Equal(2, movements.Count);
        Assert.Equal(0m, movements.Sum(transaction => transaction.Amount));
        Assert.All(movements, movement => Assert.Equal(transferId, movement.TransferId));
    }
}
