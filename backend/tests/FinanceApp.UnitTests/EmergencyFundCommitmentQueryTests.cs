using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.UnitTests;

/// <summary>
/// Cobertura de GetScheduledCommitmentByCycleAsync, que alimenta
/// CurrentDashboardDto.CycleReplenishmentCommitment desde el único
/// compromiso de devolución que sobrevive: restaurar el fondo de emergencia.
/// </summary>
public class EmergencyFundCommitmentQueryTests
{
    private static readonly DateOnly CycleEnd = new(2026, 8, 31);

    [Fact]
    public async Task GetScheduledCommitmentByCycleAsync_IncludesDueAndOverdue_ExcludesLaterCycles()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = await SeedUserWithFundAsync(context);

        // Vence dentro del ciclo → cuenta completa.
        AddRestoration(context, user, scheduled: 100m, original: 600m, restored: 0m,
            nextScheduled: new DateOnly(2026, 8, 20));
        // Cuota vencida en un ciclo anterior → sigue siendo compromiso vivo.
        AddRestoration(context, user, scheduled: 50m, original: 300m, restored: 0m,
            nextScheduled: new DateOnly(2026, 7, 10));
        // Vence en el ciclo siguiente → fuera.
        AddRestoration(context, user, scheduled: 999m, original: 999m, restored: 0m,
            nextScheduled: new DateOnly(2026, 9, 15));
        await context.SaveChangesAsync();

        var repository = new EmergencyFundRestorationRepository(context);
        var commitment = await repository.GetScheduledCommitmentByCycleAsync(user.Id, CycleEnd);

        Assert.Equal(150m, commitment);
    }

    [Fact]
    public async Task GetScheduledCommitmentByCycleAsync_CapsByOutstanding_AndIgnoresClosed()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = await SeedUserWithFundAsync(context);

        // Última cuota: quedan 30 pendientes aunque la cuota programada sea 100.
        AddRestoration(context, user, scheduled: 100m, original: 500m, restored: 470m,
            nextScheduled: new DateOnly(2026, 8, 20));
        // Completada → no es compromiso.
        AddRestoration(context, user, scheduled: 200m, original: 200m, restored: 200m,
            nextScheduled: new DateOnly(2026, 8, 20),
            status: EmergencyFundRestorationStatus.Completed);
        await context.SaveChangesAsync();

        var repository = new EmergencyFundRestorationRepository(context);
        var commitment = await repository.GetScheduledCommitmentByCycleAsync(user.Id, CycleEnd);

        Assert.Equal(30m, commitment);
    }

    private static async Task<User> SeedUserWithFundAsync(AppDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "commitment@example.test",
            PasswordHash = "hash",
            FirstName = "Commitment",
            LastName = "Test"
        };
        var savings = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Cuenta de ahorro",
            Type = FinancialAccountType.Savings,
            CurrentBalance = 5_000m,
            IsActive = true
        };
        var goal = new SavingsGoal
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Fondo de emergencia",
            Purpose = SavingsGoalPurpose.EmergencyFund,
            TargetAmount = 5_000m,
            CurrentAmount = 5_000m,
            MinimumProtectedAmount = 1_000m,
            SavingsAccountId = savings.Id
        };
        context.AddRange(user, savings, goal);
        await context.SaveChangesAsync();

        return user;
    }

    private static void AddRestoration(
        AppDbContext context, User user, decimal scheduled, decimal original,
        decimal restored, DateOnly nextScheduled,
        EmergencyFundRestorationStatus status = EmergencyFundRestorationStatus.Open)
    {
        var goal = context.SavingsGoals.Local.First(g => g.UserId == user.Id);
        var withdrawal = new SavingsGoalWithdrawal
        {
            Id = Guid.NewGuid(),
            SavingsGoalId = goal.Id,
            WithdrawalDate = nextScheduled,
            Amount = original,
            Reason = SavingsWithdrawalReason.TemporaryLoan
        };
        context.Add(withdrawal);
        context.Add(new EmergencyFundRestoration
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SavingsGoalId = goal.Id,
            SourceWithdrawalId = withdrawal.Id,
            Description = "Uso extraordinario",
            AcquisitionDate = new DateOnly(2026, 6, 1),
            OriginalAmount = original,
            RestoredAmount = restored,
            TargetRestorationDate = new DateOnly(2026, 12, 1),
            ScheduledContributionAmount = scheduled,
            NextScheduledDate = nextScheduled,
            Status = status
        });
    }
}
