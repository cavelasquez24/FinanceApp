using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.UnitTests;

public class ReimbursementRepositoryTests
{
    [Fact]
    public async Task Totals_AccumulatePartialRefunds_AndIgnoreVoidedRecords()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = Guid.NewGuid(), Email = "refund@example.test", PasswordHash = "x",
            FirstName = "Refund", LastName = "User"
        };
        var category = new Category { Id = Guid.NewGuid(), UserId = user.Id, Name = "Prueba" };
        var expense = new Expense
        {
            Id = Guid.NewGuid(), UserId = user.Id, CategoryId = category.Id,
            Amount = 4.59m, Date = new DateOnly(2026, 8, 1)
        };
        context.AddRange(user, category, expense);
        context.Reimbursements.AddRange(
            Refund(user.Id, expense.Id, 2m, new DateOnly(2026, 8, 5)),
            Refund(user.Id, expense.Id, 2.56m, new DateOnly(2026, 8, 6)),
            Refund(user.Id, expense.Id, 1m, new DateOnly(2026, 8, 6), DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();

        var repository = new ReimbursementRepository(context);

        Assert.Equal(4.56m, await repository.GetTotalByExpenseIdAsync(user.Id, expense.Id, null));
        Assert.Equal(4.56m, await repository.GetTotalByDateRangeAsync(
            user.Id, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)));
    }

    private static Reimbursement Refund(Guid userId, Guid expenseId, decimal amount,
        DateOnly date, DateTimeOffset? deletedAt = null) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, ExpenseId = expenseId,
        DestinationType = ReimbursementDestinationType.Account,
        Amount = amount, Date = date, IdempotencyKey = Guid.NewGuid(), DeletedAt = deletedAt
    };
}

