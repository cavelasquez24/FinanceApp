using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.UnitTests;

public class SavingsReplenishmentServiceTests
{
    private sealed class Fixture
    {
        public FakeSavingsGoalRepository GoalRepo { get; } = new();
        public FakeSavingsReplenishmentRepository ReplenishmentRepo { get; } = new();
        public FakeFinancialAccountService AccountService { get; } = new();
        public FakeUserRepository UserRepo { get; } = new();
        public MutableBusinessDateProvider DateProvider { get; } = new() { Today = new DateOnly(2026, 8, 23) };
        public SavingsReplenishmentService Service { get; }

        public Fixture() =>
            Service = new SavingsReplenishmentService(
                GoalRepo, ReplenishmentRepo, AccountService, UserRepo, DateProvider, new PassThroughUnitOfWork());
    }

    private static SavingsGoal NewGoal(decimal current = 200m, decimal target = 500m) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "Fondo Emergencia",
        TargetAmount = target,
        CurrentAmount = current,
        SavingsAccountId = Guid.NewGuid()
    };

    private static FinancialAccountResponseDto Account(Guid id, string type, decimal balance) => new()
    {
        Id = id,
        Name = type == "cash" ? "Cuenta principal" : "Ahorros",
        Type = type,
        CurrentBalance = balance,
        IsActive = true
    };

    private static SavingsReplenishment NewPlan(
        SavingsGoal goal, FinancialAccountResponseDto sourceAccount,
        decimal amountTaken = 100m, decimal monthlyDebit = 40m,
        decimal amountReplenished = 0m, ReplenishmentStatus status = ReplenishmentStatus.Active,
        bool autoDebitEnabled = true, bool isPaused = false, DateOnly? lastDebitAt = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = goal.UserId,
        SavingsGoalId = goal.Id,
        SourceAccountId = sourceAccount.Id,
        SourceAccount = new FinancialAccount
        {
            Id = sourceAccount.Id,
            Name = sourceAccount.Name,
            Type = FinancialAccountType.Cash
        },
        Name = "Reposición emergencia",
        AmountTaken = amountTaken,
        MonthlyDebitAmount = monthlyDebit,
        AmountReplenished = amountReplenished,
        AutoDebitEnabled = autoDebitEnabled,
        IsPaused = isPaused,
        Status = status,
        LastDebitAt = lastDebitAt,
        SavingsGoal = goal
    };

    // Prepara un fixture con una meta, cuenta de ahorro, cuenta operativa y
    // opcionalmente un plan ya cargado en los repos — reduce boilerplate
    // repetido en cada caso.
    private static (Fixture fx, SavingsGoal goal, FinancialAccountResponseDto savingsAccount, FinancialAccountResponseDto sourceAccount) Setup(
        decimal sourceBalance = 1000m, decimal goalCurrent = 200m)
    {
        var fx = new Fixture();
        var goal = NewGoal(current: goalCurrent);
        var savingsAccount = Account(goal.SavingsAccountId!.Value, "savings", goalCurrent);
        var sourceAccount = Account(Guid.NewGuid(), "cash", sourceBalance);
        fx.GoalRepo.Stored = goal;
        fx.AccountService.Accounts.AddRange([savingsAccount, sourceAccount]);
        return (fx, goal, savingsAccount, sourceAccount);
    }

    // 1. Crear plan → sin cambios de saldo, Active, AmountReplenished = 0
    [Fact]
    public async Task CreateAsync_DoesNotMoveAnyBalance_AndStartsActive()
    {
        var (fx, goal, _, sourceAccount) = Setup();

        var result = await fx.Service.CreateAsync(goal.UserId, new SavingsReplenishmentCreateDto
        {
            SavingsGoalId = goal.Id,
            SourceAccountId = sourceAccount.Id,
            Name = "Reposición emergencia",
            AmountTaken = 100m,
            MonthlyDebitAmount = 40m
        });

        Assert.Equal(0m, result.AmountReplenished);
        Assert.Equal(ReplenishmentStatus.Active, result.Status);
        Assert.Equal(100m, result.PendingAmount);
        Assert.Equal(1000m, sourceAccount.CurrentBalance);
        Assert.Equal(200m, goal.CurrentAmount);
    }

    // 2. Débito automático ciclo 1
    [Fact]
    public async Task ExecuteCycleDebits_FirstCycle_MovesBalanceAndIncrementsGoal()
    {
        var (fx, goal, _, sourceAccount) = Setup();
        var plan = NewPlan(goal, sourceAccount);
        fx.ReplenishmentRepo.Items.Add(plan);
        fx.GoalRepo.LinkedReplenishment = plan;

        var result = await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(40m, plan.AmountReplenished);
        Assert.Equal(960m, sourceAccount.CurrentBalance);
        Assert.Equal(240m, goal.CurrentAmount);
        Assert.Equal(fx.DateProvider.Today, plan.LastDebitAt);
    }

    // 3. Débito automático ciclo 2 — acumula sobre el anterior
    [Fact]
    public async Task ExecuteCycleDebits_SecondCycle_AccumulatesOverPreviousDebit()
    {
        var (fx, goal, _, sourceAccount) = Setup();
        var plan = NewPlan(goal, sourceAccount);
        fx.ReplenishmentRepo.Items.Add(plan);
        fx.GoalRepo.LinkedReplenishment = plan;

        await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);     // ciclo de agosto
        fx.DateProvider.Today = new DateOnly(2026, 9, 1);          // avanza al ciclo de septiembre
        var result = await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(80m, plan.AmountReplenished);
        Assert.Equal(920m, sourceAccount.CurrentBalance);
        Assert.Equal(280m, goal.CurrentAmount);
    }

    // 4. Último débito usa Min(monthly, pending) → Status = Completed
    [Fact]
    public async Task ExecuteCycleDebits_LastDebit_UsesRemainingPending_AndCompletes()
    {
        var (fx, goal, _, sourceAccount) = Setup();
        var plan = NewPlan(goal, sourceAccount, amountTaken: 100m, monthlyDebit: 40m, amountReplenished: 80m);
        fx.ReplenishmentRepo.Items.Add(plan);
        fx.GoalRepo.LinkedReplenishment = plan;

        var result = await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(100m, plan.AmountReplenished);
        Assert.Equal(0m, plan.PendingAmount);
        Assert.Equal(ReplenishmentStatus.Completed, plan.Status);
        Assert.Equal(fx.DateProvider.Today, plan.CompletedAt);
        Assert.Equal(980m, sourceAccount.CurrentBalance);  // solo se debitaron 20, no 40
        Assert.Equal(220m, goal.CurrentAmount);
    }

    // 5. Sin fondos en SourceAccount → no ejecuta, aparece en InsufficientFunds
    [Fact]
    public async Task ExecuteCycleDebits_InsufficientFunds_SkipsAndReportsFailure_WithoutThrowing()
    {
        var (fx, goal, _, sourceAccount) = Setup(sourceBalance: 10m);
        var plan = NewPlan(goal, sourceAccount, amountTaken: 100m, monthlyDebit: 40m);
        fx.ReplenishmentRepo.Items.Add(plan);
        fx.GoalRepo.LinkedReplenishment = plan;

        var result = await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);

        Assert.Equal(0, result.ProcessedCount);
        var failure = Assert.Single(result.InsufficientFunds);
        Assert.Equal(plan.Id, failure.ReplenishmentId);
        Assert.Equal(plan.Name, failure.ReplenishmentName);
        Assert.Equal(40m, failure.RequiredAmount);
        Assert.Equal(10m, failure.AvailableBalance);

        Assert.Equal(ReplenishmentStatus.Active, plan.Status);
        Assert.Equal(0m, plan.AmountReplenished);
        Assert.Equal(10m, sourceAccount.CurrentBalance);
        Assert.Equal(200m, goal.CurrentAmount);
    }

    // 6. Aporte manual adelanta el progreso y completa si llega al total
    [Fact]
    public async Task ApplyManualDebitAsync_CompletesPlan_WhenReachingAmountTaken()
    {
        var (fx, goal, _, sourceAccount) = Setup();
        var plan = NewPlan(goal, sourceAccount, amountTaken: 100m, monthlyDebit: 40m, amountReplenished: 70m);
        fx.ReplenishmentRepo.Items.Add(plan);
        fx.GoalRepo.LinkedReplenishment = plan;

        var result = await fx.Service.ApplyManualDebitAsync(goal.UserId, plan.Id, new SavingsReplenishmentManualDebitDto
        {
            Amount = 30m,
            IdempotencyKey = Guid.NewGuid()
        });

        Assert.Equal(100m, result.AmountReplenished);
        Assert.Equal(0m, result.PendingAmount);
        Assert.Equal(ReplenishmentStatus.Completed, result.Status);
        Assert.Equal(970m, sourceAccount.CurrentBalance);
        Assert.Equal(230m, goal.CurrentAmount);

        var contribution = Assert.Single(fx.GoalRepo.AddedContributions);
        Assert.Equal(DebitType.Manual, contribution.DebitType);
        Assert.Equal(plan.Id, contribution.SavingsReplenishmentId);
    }

    // 7. Pausa → el ciclo no lo procesa
    [Fact]
    public async Task PauseAsync_ExcludesPlanFromCycleDebits()
    {
        var (fx, goal, _, sourceAccount) = Setup();
        var plan = NewPlan(goal, sourceAccount);
        fx.ReplenishmentRepo.Items.Add(plan);
        fx.GoalRepo.LinkedReplenishment = plan;

        var paused = await fx.Service.PauseAsync(goal.UserId, plan.Id, new SavingsReplenishmentPauseDto { Reason = "Viaje" });
        Assert.True(paused.IsPaused);
        Assert.Equal(ReplenishmentStatus.Paused, paused.Status);

        var result = await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);

        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0m, plan.AmountReplenished);
        Assert.Equal(1000m, sourceAccount.CurrentBalance);
        Assert.Equal(200m, goal.CurrentAmount);
    }

    // 8. Reanudar → el ciclo vuelve a procesarlo
    [Fact]
    public async Task ResumeAsync_MakesPlanEligibleForCycleDebitsAgain()
    {
        var (fx, goal, _, sourceAccount) = Setup();
        var plan = NewPlan(goal, sourceAccount);
        fx.ReplenishmentRepo.Items.Add(plan);
        fx.GoalRepo.LinkedReplenishment = plan;

        await fx.Service.PauseAsync(goal.UserId, plan.Id, new SavingsReplenishmentPauseDto());
        var resumed = await fx.Service.ResumeAsync(goal.UserId, plan.Id);
        Assert.False(resumed.IsPaused);
        Assert.Equal(ReplenishmentStatus.Active, resumed.Status);

        var result = await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(40m, plan.AmountReplenished);
        Assert.Equal(960m, sourceAccount.CurrentBalance);
        Assert.Equal(240m, goal.CurrentAmount);
    }

    // 9. Cancelar → el ciclo no lo procesa
    [Fact]
    public async Task CancelAsync_ExcludesPlanFromCycleDebits()
    {
        var (fx, goal, _, sourceAccount) = Setup();
        var plan = NewPlan(goal, sourceAccount);
        fx.ReplenishmentRepo.Items.Add(plan);
        fx.GoalRepo.LinkedReplenishment = plan;

        await fx.Service.CancelAsync(goal.UserId, plan.Id);
        Assert.Equal(ReplenishmentStatus.Cancelled, plan.Status);

        var result = await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);

        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0m, plan.AmountReplenished);
        Assert.Equal(1000m, sourceAccount.CurrentBalance);
        Assert.Equal(200m, goal.CurrentAmount);
    }

    // 10. Patrimonio invariante: sourceAccount + goal.CurrentAmount no cambia
    // ni al crear el plan ni al ejecutar débitos (solo redistribuye).
    [Fact]
    public async Task CreateAndExecuteCycleDebits_NeverChangeCombinedWealth()
    {
        var (fx, goal, _, sourceAccount) = Setup();
        decimal CombinedWealth() => sourceAccount.CurrentBalance + goal.CurrentAmount;
        var wealthBefore = CombinedWealth();

        var created = await fx.Service.CreateAsync(goal.UserId, new SavingsReplenishmentCreateDto
        {
            SavingsGoalId = goal.Id,
            SourceAccountId = sourceAccount.Id,
            Name = "Reposición emergencia",
            AmountTaken = 100m,
            MonthlyDebitAmount = 40m
        });
        Assert.Equal(wealthBefore, CombinedWealth());

        var plan = fx.ReplenishmentRepo.Items.Single(r => r.Id == created.Id);
        plan.SavingsGoal = goal;
        fx.GoalRepo.LinkedReplenishment = plan;

        await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);
        Assert.Equal(wealthBefore, CombinedWealth());

        fx.DateProvider.Today = new DateOnly(2026, 9, 1);
        await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);
        Assert.Equal(wealthBefore, CombinedWealth());
    }

    // 11. Idempotencia — ejecutar el mismo ciclo dos veces no duplica el débito
    [Fact]
    public async Task ExecuteCycleDebits_CalledTwiceInSameCycle_DoesNotDuplicateDebit()
    {
        var (fx, goal, _, sourceAccount) = Setup();
        var plan = NewPlan(goal, sourceAccount);
        fx.ReplenishmentRepo.Items.Add(plan);
        fx.GoalRepo.LinkedReplenishment = plan;

        var first = await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);
        var second = await fx.Service.ExecuteCycleDebitsAsync(goal.UserId);

        Assert.Equal(1, first.ProcessedCount);
        Assert.Equal(0, second.ProcessedCount);
        Assert.Equal(1, second.SkippedAlreadyDebitedCount);
        Assert.Equal(40m, plan.AmountReplenished);   // no se duplicó
        Assert.Equal(960m, sourceAccount.CurrentBalance);
        Assert.Equal(240m, goal.CurrentAmount);
    }

    // ── Test doubles ─────────────────────────────────────────────────────────

    private sealed class MutableBusinessDateProvider : IBusinessDateProvider
    {
        public DateOnly Today { get; set; }
        public DateOnly GetDate(DateTimeOffset instant) => Today;
    }

    private sealed class PassThroughUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) => action(cancellationToken);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? Stored { get; set; }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored?.Id == id ? Stored : null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> CreateAsync(User entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> UpdateAsync(User entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(User entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>
    /// Guarda una única SavingsGoal (Stored) y, cuando AddContributionAsync
    /// recibe un aporte vinculado a LinkedReplenishment, replica el fixup
    /// de navegación que EF haría automáticamente (Contributions de la
    /// meta y del plan quedan sincronizadas).
    /// </summary>
    private sealed class FakeSavingsGoalRepository : ISavingsGoalRepository
    {
        public SavingsGoal? Stored { get; set; }
        public SavingsReplenishment? LinkedReplenishment { get; set; }
        public List<SavingsGoalContribution> AddedContributions { get; } = new();

        public Task<SavingsGoal?> GetByIdWithHistoryAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored?.Id == id && Stored.UserId == userId ? Stored : null);

        public Task AddContributionAsync(SavingsGoalContribution contribution, CancellationToken cancellationToken = default)
        {
            AddedContributions.Add(contribution);
            Stored?.Contributions.Add(contribution);
            if (contribution.SavingsReplenishmentId.HasValue
                && LinkedReplenishment?.Id == contribution.SavingsReplenishmentId)
                LinkedReplenishment.Contributions.Add(contribution);
            return Task.CompletedTask;
        }

        public Task<SavingsGoal> UpdateAsync(SavingsGoal entity, CancellationToken cancellationToken = default)
        {
            Stored = entity;
            return Task.FromResult(entity);
        }

        public Task<SavingsGoal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored?.Id == id ? Stored : null);
        public Task<IEnumerable<SavingsGoal>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<SavingsGoal>>(Stored is null ? [] : [Stored]);
        public Task<SavingsGoal> CreateAsync(SavingsGoal entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(SavingsGoal entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<SavingsGoal>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> GetTotalSavedAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddWithdrawalAsync(SavingsGoalWithdrawal withdrawal, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> GetTotalContributionsByDateRangeAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> GetTotalCashFlowWithdrawalsByDateRangeAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> GetTotalConsumedWithdrawalsByDateRangeAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> GetAvgMonthlyContributionAsync(Guid goalId, int months, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeSavingsReplenishmentRepository : ISavingsReplenishmentRepository
    {
        public List<SavingsReplenishment> Items { get; } = new();

        public Task<SavingsReplenishment> CreateAsync(SavingsReplenishment entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<SavingsReplenishment> UpdateAsync(SavingsReplenishment entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(entity);

        public Task<SavingsReplenishment?> GetOwnedByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(r => r.Id == id && r.UserId == userId && !r.IsDeleted));

        public Task<IReadOnlyList<SavingsReplenishment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavingsReplenishment>>(
                Items.Where(r => r.UserId == userId && !r.IsDeleted).ToList());

        public Task<IReadOnlyList<SavingsReplenishment>> GetByGoalIdAsync(Guid goalId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavingsReplenishment>>(
                Items.Where(r => r.SavingsGoalId == goalId && r.UserId == userId && !r.IsDeleted).ToList());

        public Task<IReadOnlyList<SavingsReplenishment>> GetActiveForAutoDebitAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavingsReplenishment>>(
                Items.Where(r => r.UserId == userId
                    && r.Status == ReplenishmentStatus.Active
                    && r.AutoDebitEnabled
                    && !r.IsPaused
                    && !r.IsDeleted).ToList());

        public Task<SavingsReplenishment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(r => r.Id == id));
        public Task<IEnumerable<SavingsReplenishment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<SavingsReplenishment>>(Items);
        public Task DeleteAsync(SavingsReplenishment entity, CancellationToken cancellationToken = default)
        {
            Items.Remove(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFinancialAccountService : IFinancialAccountService
    {
        public List<FinancialAccountResponseDto> Accounts { get; } = new();

        public Task<IReadOnlyList<FinancialAccountResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinancialAccountResponseDto>>(Accounts);

        public Task SyncTransferBetweenAccountsAsync(
            Guid userId, Guid? fromAccountId, FinancialAccountType fromFallbackType,
            Guid? toAccountId, FinancialAccountType toFallbackType,
            decimal amount, DateOnly date, string sourceType, Guid sourceId,
            string description, CancellationToken cancellationToken = default)
        {
            var from = Accounts.SingleOrDefault(a => a.Id == fromAccountId);
            if (from != null) from.CurrentBalance -= amount;
            var to = Accounts.SingleOrDefault(a => a.Id == toAccountId);
            if (to != null) to.CurrentBalance += amount;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AccountTransactionResponseDto>> GetRecentTransactionsAsync(Guid userId, int count, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FinancialAccountResponseDto> CreateAsync(Guid userId, FinancialAccountCreateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FinancialAccountResponseDto> UpdateAsync(Guid id, Guid userId, FinancialAccountUpdateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AccountTransferResponseDto> TransferAsync(Guid userId, AccountTransferCreateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FinancialAccountResponseDto> GetOrCreateDefaultAsync(Guid userId, FinancialAccountType type, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> GetAvailableBalanceAsync(Guid userId, Guid? accountId, FinancialAccountType fallbackType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SyncMovementAsync(Guid userId, Guid? accountId, FinancialAccountType fallbackType, decimal signedAmount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SyncTransferAsync(Guid userId, FinancialAccountType fromType, FinancialAccountType toType, decimal amount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
