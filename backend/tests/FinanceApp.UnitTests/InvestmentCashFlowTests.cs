using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.Investment;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace FinanceApp.UnitTests;

public class InvestmentCashFlowTests
{
    [Fact]
    public async Task CyclePurchase_CreatesCashContribution_AndSetsBasisAndValue()
    {
        var repository = new FakeInvestmentRepository();
        var service = new InvestmentService(repository);
        var purchaseDate = new DateOnly(2026, 7, 23);

        var result = await service.CreateAsync(Guid.NewGuid(), new InvestmentCreateDto
        {
            Name = "ETF",
            Type = "etf",
            InitialAmount = 50m,
            CurrentValue = 12m,
            PurchaseDate = purchaseDate,
            IsHistoricalImport = false
        });

        Assert.Equal(50m, result.InitialAmount);
        Assert.Equal(50m, result.CurrentValue);
        var contribution = Assert.Single(repository.Stored!.Contributions);
        Assert.Equal(50m, contribution.Amount);
        Assert.Equal(purchaseDate, contribution.ContributionDate);
    }

    [Fact]
    public async Task HistoricalImport_SetsSnapshot_WithoutCashContribution()
    {
        var repository = new FakeInvestmentRepository();
        var service = new InvestmentService(repository);

        var result = await service.CreateAsync(Guid.NewGuid(), new InvestmentCreateDto
        {
            Name = "ETF histórico",
            Type = "etf",
            InitialAmount = 50m,
            CurrentValue = 49.20m,
            PurchaseDate = new DateOnly(2025, 1, 1),
            IsHistoricalImport = true
        });

        Assert.Equal(50m, result.InitialAmount);
        Assert.Equal(49.20m, result.CurrentValue);
        Assert.Empty(repository.Stored!.Contributions);
    }

    [Fact]
    public async Task LaterContribution_RaisesCapitalAndValue_WithoutTouchingUnrealizedGain()
    {
        var investment = NewInvestment(initialAmount: 50m, currentValue: 49.20m);
        var repository = new FakeInvestmentRepository { Stored = investment };
        var service = new InvestmentService(repository);

        await service.AddContributionAsync(investment.Id, investment.UserId,
            new InvestmentContributionCreateDto { Amount = 10m });

        // Un aporte compra participaciones: sube el coste y el valor por igual.
        // Lo que no puede hacer es crear ni destruir plusvalía latente, que sigue
        // siendo -0,80 antes y después. Los movimientos de precio son AddRecordAsync.
        Assert.Equal(60m, investment.InitialAmount);
        Assert.Equal(59.20m, investment.CurrentValue);
        Assert.Equal(-0.80m, investment.CurrentValue - investment.InitialAmount);
        Assert.Equal(10m, repository.AddedContribution!.Amount);
    }

    [Fact]
    public async Task Valuation_ChangesOnlyMarketValue()
    {
        var investment = NewInvestment(initialAmount: 50m, currentValue: 50m);
        var repository = new FakeInvestmentRepository { Stored = investment };
        var service = new InvestmentService(repository);

        await service.AddRecordAsync(investment.Id, investment.UserId,
            new InvestmentRecordCreateDto
            {
                RecordDate = new DateOnly(2026, 7, 23),
                Value = 49.20m
            });

        Assert.Equal(50m, investment.InitialAmount);
        Assert.Equal(49.20m, investment.CurrentValue);
        Assert.Null(repository.AddedContribution);
    }

    [Fact]
    public void Income100_AndInvestmentPurchase50_LeavesResidual50()
    {
        var residual = CashFlowCalculator.CalculateResidual(
            income: 100m,
            consumptionExpenses: 0m,
            savingsContributions: 0m,
            savingsWithdrawals: 0m,
            investmentContributions: 50m,
            debtPrincipalPaid: 0m);

        Assert.Equal(50m, residual);
    }

    [Fact]
    public void SavingsWithdrawal_ReleasesCashBackToResidual()
    {
        var residual = CashFlowCalculator.CalculateResidual(
            income: 100m,
            consumptionExpenses: 0m,
            savingsContributions: 30m,
            savingsWithdrawals: 10m,
            investmentContributions: 0m,
            debtPrincipalPaid: 0m);

        Assert.Equal(80m, residual);
    }

    [Fact]
    public void WithdrawalDto_AcceptsFrontendReasonName()
    {
        const string json = """{"amount":10,"reason":"ReallocatedToLiquid"}""";
        var dto = JsonSerializer.Deserialize<SavingsGoalWithdrawalCreateDto>(json, JsonSerializerOptions.Web);

        Assert.NotNull(dto);
        Assert.Equal(SavingsWithdrawalReason.ReallocatedToLiquid, dto.Reason);
    }
    // --- Patrimony / FinancialPosition tests ---

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void NetWorth_UsesInvestmentPositions_NotAggregateAccount()
    {
        var result = FinancialPositionService.CalculateCurrentSnapshot(
            Today,
            accounts: [Acct(FinancialAccountType.Investment, 200m), Acct(FinancialAccountType.Cash, 500m)],
            investments: [Inv(currentValue: 101.59m, costBasis: 101.59m)],
            debts: [],
            cards: [],
            savingsGoalAllocations: 0m);

        Assert.Equal(601.59m, result.NetWorth);
        Assert.Equal(101.59m, result.Assets.Investments);
        Assert.Contains(result.Warnings, w => w.Code == "INVESTMENT_LEDGER_DIFFERENCE");
    }

    [Fact]
    public void NetWorth_WhenPositionsMatchAccount_NoLedgerWarning()
    {
        var result = FinancialPositionService.CalculateCurrentSnapshot(
            Today,
            accounts: [Acct(FinancialAccountType.Investment, 101.59m), Acct(FinancialAccountType.Cash, 500m)],
            investments: [Inv(currentValue: 101.59m, costBasis: 101.59m)],
            debts: [],
            cards: [],
            savingsGoalAllocations: 0m);

        Assert.Equal(601.59m, result.NetWorth);
        Assert.DoesNotContain(result.Warnings, w => w.Code == "INVESTMENT_LEDGER_DIFFERENCE");
    }

    [Fact]
    public void Contribution_DoesNotReduceNetWorth()
    {
        var before = FinancialPositionService.CalculateCurrentSnapshot(
            Today,
            accounts: [Acct(FinancialAccountType.Cash, 600m)],
            investments: [],
            debts: [], cards: [], savingsGoalAllocations: 0m);

        var after = FinancialPositionService.CalculateCurrentSnapshot(
            Today,
            accounts: [Acct(FinancialAccountType.Cash, 500m)],
            investments: [Inv(currentValue: 100m, costBasis: 100m)],
            debts: [], cards: [], savingsGoalAllocations: 0m);

        Assert.Equal(before.NetWorth, after.NetWorth);
    }

    [Fact]
    public void Valuation_IncreasesNetWorth_NotContributedCapital()
    {
        var result = FinancialPositionService.CalculateCurrentSnapshot(
            Today,
            accounts: [Acct(FinancialAccountType.Cash, 500m)],
            investments: [Inv(currentValue: 115m, costBasis: 100m)],
            debts: [], cards: [], savingsGoalAllocations: 0m);

        Assert.Equal(115m, result.Assets.InvestmentPositions);
        Assert.Equal(100m, result.Assets.InvestmentCostBasis);
        Assert.Equal(15m, result.Assets.InvestmentUnrealizedGainLoss);
        Assert.Equal(615m, result.NetWorth);
    }

    private static FinancialAccount Acct(FinancialAccountType type, decimal balance) => new()
    {
        Type = type,
        CurrentBalance = balance,
        IsActive = true
    };

    private static Investment Inv(decimal currentValue, decimal costBasis) => new()
    {
        InitialAmount = costBasis,
        CurrentValue = currentValue,
        IsActive = true
    };

    private static Investment NewInvestment(decimal initialAmount, decimal currentValue) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "ETF",
        Type = InvestmentType.ETF,
        InitialAmount = initialAmount,
        CurrentValue = currentValue,
        PurchaseDate = new DateOnly(2026, 7, 1)
    };

    /// <summary>
    /// Reproduce el caso real del Fondo Acumulación: importación, aporte y
    /// valorización posterior. Si el aporte no sube CurrentValue, la valorización
    /// se calcula contra un valor rancio y vuelve a acreditar el aporte al ledger.
    /// </summary>
    [Fact]
    public async Task ContributionThenValuation_KeepsLedgerInSyncWithPositionValue()
    {
        var repository = new FakeInvestmentRepository();
        var ledger = new LedgerRecordingAccountService();
        var service = new InvestmentService(repository, ledger, new PassThroughUnitOfWork());
        var userId = Guid.NewGuid();

        var created = await service.CreateAsync(userId, new InvestmentCreateDto
        {
            Name = "Fondo Acumulación",
            Type = "etf",
            InitialAmount = 4382.24m,
            CurrentValue = 4382.24m,
            PurchaseDate = new DateOnly(2026, 8, 18),
            IsHistoricalImport = false
        });

        await service.AddContributionAsync(created.Id, userId, new InvestmentContributionCreateDto
        {
            Amount = 123.50m,
            ContributionDate = new DateOnly(2026, 8, 26)
        });

        Assert.Equal(4505.74m, repository.Stored!.InitialAmount);
        Assert.Equal(4505.74m, repository.Stored.CurrentValue);
        Assert.Equal(4505.74m, ledger.InvestmentBalance);

        await service.AddRecordAsync(created.Id, userId, new InvestmentRecordCreateDto
        {
            RecordDate = new DateOnly(2026, 8, 26),
            Value = 4512.03m
        });

        Assert.Equal(4512.03m, repository.Stored.CurrentValue);
        Assert.Equal(repository.Stored.CurrentValue, ledger.InvestmentBalance);
    }

    /// <summary>Acumula en un solo número lo que el servicio manda a la cuenta de inversión.</summary>
    private sealed class LedgerRecordingAccountService : IFinancialAccountService
    {
        public decimal InvestmentBalance { get; private set; }

        public Task<FinancialAccountResponseDto> GetOrCreateDefaultAsync(
            Guid userId, FinancialAccountType type, CancellationToken cancellationToken = default)
            => Task.FromResult(new FinancialAccountResponseDto());

        public Task SyncTransferAsync(
            Guid userId, FinancialAccountType fromType, FinancialAccountType toType,
            decimal amount, DateOnly date, string sourceType, Guid sourceId,
            string description, CancellationToken cancellationToken = default)
        {
            if (toType == FinancialAccountType.Investment) InvestmentBalance += amount;
            if (fromType == FinancialAccountType.Investment) InvestmentBalance -= amount;
            return Task.CompletedTask;
        }

        public Task SyncMovementAsync(
            Guid userId, Guid? accountId, FinancialAccountType fallbackType,
            decimal signedAmount, DateOnly date, string sourceType, Guid sourceId,
            string description, CancellationToken cancellationToken = default)
        {
            if (fallbackType == FinancialAccountType.Investment) InvestmentBalance += signedAmount;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FinancialAccountResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<AccountTransactionResponseDto>> GetRecentTransactionsAsync(Guid userId, int count, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<FinancialAccountResponseDto> CreateAsync(Guid userId, FinancialAccountCreateDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<FinancialAccountResponseDto> UpdateAsync(Guid id, Guid userId, FinancialAccountUpdateDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountTransferResponseDto> TransferAsync(Guid userId, AccountTransferCreateDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetAvailableBalanceAsync(Guid userId, Guid? accountId, FinancialAccountType fallbackType, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SyncTransferBetweenAccountsAsync(Guid userId, Guid? fromAccountId, FinancialAccountType fromFallbackType, Guid? toAccountId, FinancialAccountType toFallbackType, decimal amount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeInvestmentRepository : IInvestmentRepository
    {
        public Investment? Stored { get; set; }
        public InvestmentContribution? AddedContribution { get; private set; }

        public Task<Investment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored?.Id == id ? Stored : null);

        public Task<IEnumerable<Investment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Investment>>(Stored is null ? [] : [Stored]);

        public Task<Investment> CreateAsync(Investment entity, CancellationToken cancellationToken = default)
        {
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            Stored = entity;
            return Task.FromResult(entity);
        }

        public Task<Investment> UpdateAsync(Investment entity, CancellationToken cancellationToken = default)
        {
            Stored = entity;
            return Task.FromResult(entity);
        }

        public Task DeleteAsync(Investment entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IEnumerable<Investment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Investment>>(Stored?.UserId == userId ? [Stored] : []);

        public Task<decimal> GetTotalCurrentValueAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored?.UserId == userId ? Stored.CurrentValue : 0m);

        public Task AddContributionAsync(InvestmentContribution contribution, CancellationToken cancellationToken = default)
        {
            AddedContribution = contribution;
            return Task.CompletedTask;
        }

        public Task<decimal> GetTotalContributionsByDateRangeAsync(
            Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task AddTransactionAsync(InvestmentTransaction transaction, CancellationToken cancellationToken = default)
        {
            if (Stored != null && Stored.Id == transaction.InvestmentId)
                Stored.Transactions.Add(transaction);
            return Task.CompletedTask;
        }
    }

    // --- Tests: ImportaciónHistórica / ConsolidatedSnapshot ---

    [Fact]
    public async Task HistoricalImport_DoesNotCreateAccountTransaction()
    {
        var repository = new FakeInvestmentRepository();
        var spy = new SpyAccountService();
        var service = new InvestmentService(repository, spy);

        await service.CreateAsync(Guid.NewGuid(), new InvestmentCreateDto
        {
            Name = "VWCE",
            Type = "etf",
            InitialAmount = 98.97m,
            CurrentValue = 102.50m,
            PurchaseDate = new DateOnly(2026, 7, 6),
            IsHistoricalImport = true,
            IsConsolidatedSnapshot = false
        });

        Assert.False(spy.SyncTransferCalled);
        Assert.False(spy.SyncMovementCalled);
    }

    [Fact]
    public async Task ConsolidatedSnapshot_SetsValueWithoutContributionOrTransaction()
    {
        var repository = new FakeInvestmentRepository();
        var service = new InvestmentService(repository);

        await service.CreateAsync(Guid.NewGuid(), new InvestmentCreateDto
        {
            Name = "Portfolio legacy",
            Type = "etf",
            InitialAmount = 4382.24m,
            CurrentValue = 4382.24m,
            PurchaseDate = new DateOnly(2024, 1, 1),
            IsHistoricalImport = true,
            IsConsolidatedSnapshot = true
        });

        Assert.Equal(4382.24m, repository.Stored!.CurrentValue);
        Assert.Empty(repository.Stored.Contributions);
        Assert.Empty(repository.Stored.Transactions);
    }

    [Fact]
    public async Task HistoricalContributions_SumBecomesContributedCapital()
    {
        var repository = new FakeInvestmentRepository();
        var service = new InvestmentService(repository);

        await service.CreateAsync(Guid.NewGuid(), new InvestmentCreateDto
        {
            Name = "VWCE",
            Type = "etf",
            InitialAmount = 0m,
            CurrentValue = 105.00m,
            PurchaseDate = new DateOnly(2026, 7, 6),
            IsHistoricalImport = true,
            IsConsolidatedSnapshot = false,
            HistoricalContributions =
            [
                new HistoricalContributionDto { ContributionDate = new DateOnly(2026, 7, 6),  Amount = 48.99m },
                new HistoricalContributionDto { ContributionDate = new DateOnly(2026, 7, 31), Amount = 49.98m }
            ]
        });

        Assert.Equal(98.97m, repository.Stored!.InitialAmount);
        Assert.Equal(2, repository.Stored.Transactions.Count);
        Assert.All(repository.Stored.Transactions, t => Assert.True(t.IsHistorical));
    }

    // --- Tests: Withdrawal ---

    [Fact]
    public async Task Withdrawal_ReducesCurrentValueAndCapital()
    {
        var investment = NewInvestment(initialAmount: 400m, currentValue: 550m);
        var repository = new FakeInvestmentRepository { Stored = investment };
        var service = new InvestmentService(repository);

        var result = await service.WithdrawAsync(investment.Id, investment.UserId,
            new InvestmentWithdrawalDto
            {
                WithdrawalAmount = 500m,
                CapitalReturned = 400m,
                Fee = 5m,
                WithdrawalDate = new DateOnly(2026, 8, 1)
            });

        Assert.Equal(50m, investment.CurrentValue);
        Assert.Equal(0m, investment.InitialAmount);
        Assert.Equal(495m, result.NetCashReceived);
    }

    [Fact]
    public async Task Withdrawal_CannotExceedCurrentValue()
    {
        var investment = NewInvestment(initialAmount: 400m, currentValue: 550m);
        var repository = new FakeInvestmentRepository { Stored = investment };
        var service = new InvestmentService(repository);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.WithdrawAsync(investment.Id, investment.UserId,
                new InvestmentWithdrawalDto
                {
                    WithdrawalAmount = 600m,
                    CapitalReturned = 400m,
                    Fee = 0m,
                    WithdrawalDate = new DateOnly(2026, 8, 1)
                }));
    }

    [Fact]
    public async Task Withdrawal_CannotReturnMoreCapitalThanContributed()
    {
        var investment = NewInvestment(initialAmount: 400m, currentValue: 550m);
        var repository = new FakeInvestmentRepository { Stored = investment };
        var service = new InvestmentService(repository);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.WithdrawAsync(investment.Id, investment.UserId,
                new InvestmentWithdrawalDto
                {
                    WithdrawalAmount = 500m,
                    CapitalReturned = 450m,
                    Fee = 0m,
                    WithdrawalDate = new DateOnly(2026, 8, 1)
                }));
    }

    [Fact]
    public async Task PartialWithdrawal_PreservesRemainingCapitalCorrectly()
    {
        var investment = NewInvestment(initialAmount: 200m, currentValue: 300m);
        var repository = new FakeInvestmentRepository { Stored = investment };
        var service = new InvestmentService(repository);

        var result = await service.WithdrawAsync(investment.Id, investment.UserId,
            new InvestmentWithdrawalDto
            {
                WithdrawalAmount = 100m,
                CapitalReturned = 80m,
                Fee = 0m,
                WithdrawalDate = new DateOnly(2026, 8, 1)
            });

        Assert.Equal(120m, investment.InitialAmount);
        Assert.Equal(200m, investment.CurrentValue);
        Assert.Equal(20m, result.RealizedGain);
    }

    private sealed class SpyAccountService : IFinancialAccountService
    {
        public bool SyncTransferCalled { get; private set; }
        public bool SyncMovementCalled { get; private set; }

        public Task<FinancialAccountResponseDto> GetOrCreateDefaultAsync(
            Guid userId, FinancialAccountType type, CancellationToken cancellationToken = default)
            => Task.FromResult(new FinancialAccountResponseDto());

        public Task SyncTransferAsync(
            Guid userId, FinancialAccountType fromType, FinancialAccountType toType,
            decimal amount, DateOnly date, string sourceType, Guid sourceId,
            string description, CancellationToken cancellationToken = default)
        {
            SyncTransferCalled = true;
            return Task.CompletedTask;
        }

        public Task SyncMovementAsync(
            Guid userId, Guid? accountId, FinancialAccountType fallbackType,
            decimal signedAmount, DateOnly date, string sourceType, Guid sourceId,
            string description, CancellationToken cancellationToken = default)
        {
            SyncMovementCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FinancialAccountResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<AccountTransactionResponseDto>> GetRecentTransactionsAsync(Guid userId, int count, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<FinancialAccountResponseDto> CreateAsync(Guid userId, FinancialAccountCreateDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<FinancialAccountResponseDto> UpdateAsync(Guid id, Guid userId, FinancialAccountUpdateDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountTransferResponseDto> TransferAsync(Guid userId, AccountTransferCreateDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetAvailableBalanceAsync(Guid userId, Guid? accountId, FinancialAccountType fallbackType, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SyncTransferBetweenAccountsAsync(Guid userId, Guid? fromAccountId, FinancialAccountType fromFallbackType, Guid? toAccountId, FinancialAccountType toFallbackType, decimal amount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
