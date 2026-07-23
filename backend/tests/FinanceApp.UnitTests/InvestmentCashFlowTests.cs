using FinanceApp.Application.DTOs.Investment;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
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
    public async Task LaterContribution_IncreasesBasisAndCurrentValue()
    {
        var investment = NewInvestment(initialAmount: 50m, currentValue: 49.20m);
        var repository = new FakeInvestmentRepository { Stored = investment };
        var service = new InvestmentService(repository);

        await service.AddContributionAsync(investment.Id, investment.UserId,
            new InvestmentContributionCreateDto { Amount = 10m });

        Assert.Equal(60m, investment.InitialAmount);
        Assert.Equal(59.20m, investment.CurrentValue);
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
    }
}
