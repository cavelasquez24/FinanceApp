using FinanceApp.Application.DTOs.Dashboard;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.UnitTests;

public class FinancialPositionServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void TransferBetweenAccounts_DoesNotChangeNetWorth()
    {
        var before = Calculate(accounts: [Account(FinancialAccountType.Cash, 100m), Account(FinancialAccountType.Savings, 50m)]);
        var after = Calculate(accounts: [Account(FinancialAccountType.Cash, 80m), Account(FinancialAccountType.Savings, 70m)]);

        Assert.Equal(before.NetWorth, after.NetWorth);
    }

    [Fact]
    public void SavingsGoalAllocation_IsInformationalAndNeverDoubleCounted()
    {
        var before = Calculate(
            accounts: [Account(FinancialAccountType.Savings, 200m)],
            goalAllocations: 0m);
        var after = Calculate(
            accounts: [Account(FinancialAccountType.Savings, 200m)],
            goalAllocations: 150m);

        Assert.Equal(200m, after.NetWorth);
        Assert.Equal(before.NetWorth, after.NetWorth);
        Assert.Equal(150m, after.SavingsGoalAllocations);
    }

    [Fact]
    public void InvestmentContribution_UsesPositionsAndDoesNotDoubleCountMirrorAccount()
    {
        var before = Calculate(accounts: [Account(FinancialAccountType.Cash, 1_000m)]);
        var after = Calculate(
            accounts:
            [
                Account(FinancialAccountType.Cash, 900m),
                Account(FinancialAccountType.Investment, 100m)
            ],
            investments: [Investment(100m)]);

        Assert.Equal(before.NetWorth, after.NetWorth);
        Assert.Equal(100m, after.Assets.Investments);
        Assert.Equal(100m, after.Assets.InvestmentAccountBalance);
        Assert.Equal(0m, after.Assets.UninvestedInvestmentCash);
    }

    [Fact]
    public void ExcessInvestmentAccountBalance_RaisesWarningButDoesNotInflateNetWorth()
    {
        var position = Calculate(
            accounts: [Account(FinancialAccountType.Investment, 125m)],
            investments: [Investment(100m)]);

        Assert.Equal(100m, position.Assets.InvestmentPositions);
        Assert.Equal(0m, position.Assets.UninvestedInvestmentCash);
        Assert.Equal(100m, position.Assets.Investments);
        Assert.Contains(position.Warnings, warning => warning.Code == "INVESTMENT_LEDGER_DIFFERENCE");
    }

    [Fact]
    public void InvestmentBreakdown_DistinguishesCostBasisAndUnrealizedGain()
    {
        var position = Calculate(investments: [Investment(125m, costBasis: 100m)]);

        Assert.Equal(100m, position.Assets.InvestmentCostBasis);
        Assert.Equal(125m, position.Assets.InvestmentPositions);
        Assert.Equal(25m, position.Assets.InvestmentUnrealizedGainLoss);
        Assert.Equal(125m, position.NetWorth);
    }

    [Fact]
    public void InactiveAssetsAndLiabilities_WithBalancesRemainIncluded()
    {
        var position = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 80m, isActive: false)],
            investments: [Investment(40m, isActive: false)],
            debts: [Debt(30m, isActive: false)]);

        Assert.Equal(90m, position.NetWorth);
        Assert.Contains(position.Warnings, warning => warning.Code == "INACTIVE_ACCOUNT_WITH_BALANCE");
        Assert.Contains(position.Warnings, warning => warning.Code == "INACTIVE_INVESTMENT_WITH_VALUE");
        Assert.Contains(position.Warnings, warning => warning.Code == "INACTIVE_DEBT_WITH_BALANCE");
    }

    [Fact]
    public void CreditCardDebtAndCredit_AreSplitWithCorrectSigns()
    {
        var position = Calculate(cards: [Card(120m), Card(-35m)]);

        Assert.Equal(35m, position.Assets.CreditCardCredits);
        Assert.Equal(120m, position.Liabilities.CreditCards);
        Assert.Equal(-85m, position.NetWorth);
    }

    [Fact]
    public void CardPurchaseReducesNetWorth_AndPrincipalPaymentDoesNotChangeItAgain()
    {
        var beforePurchase = Calculate(accounts: [Account(FinancialAccountType.Cash, 1_000m)]);
        var afterPurchase = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 1_000m)],
            cards: [Card(100m)]);
        var afterPrincipalPayment = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 900m)],
            cards: [Card(0m)]);

        Assert.Equal(beforePurchase.NetWorth - 100m, afterPurchase.NetWorth);
        Assert.Equal(afterPurchase.NetWorth, afterPrincipalPayment.NetWorth);
    }

    [Fact]
    public void InterestOrCommission_ReducesNetWorth()
    {
        var before = Calculate(accounts: [Account(FinancialAccountType.Cash, 100m)]);
        var after = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 100m)],
            cards: [Card(7.25m)]);

        Assert.Equal(before.NetWorth - 7.25m, after.NetWorth);
    }

    [Fact]
    public void Refund_IncreasesNetWorthWhetherItArrivesToAccountOrCard()
    {
        var original = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 100m)],
            cards: [Card(80m)]);
        var accountRefund = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 125m)],
            cards: [Card(80m)]);
        var cardRefund = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 100m)],
            cards: [Card(55m)]);

        Assert.Equal(original.NetWorth + 25m, accountRefund.NetWorth);
        Assert.Equal(original.NetWorth + 25m, cardRefund.NetWorth);
    }

    [Fact]
    public void DebtPrincipalPayment_ReducesCashAndDebtEqually()
    {
        var before = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 500m)],
            debts: [Debt(300m)]);
        var after = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 400m)],
            debts: [Debt(200m)]);

        Assert.Equal(before.NetWorth, after.NetWorth);
    }

    [Fact]
    public void BothDashboardContracts_ReadExactlyTheSameCanonicalPosition()
    {
        var position = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 400m)],
            investments: [Investment(150m)],
            cards: [Card(25m)]);
        var overview = new DashboardOverviewDto { FinancialPosition = position };
        var current = new CurrentDashboardDto { FinancialPosition = position };

        Assert.Equal(overview.NetWorthAsOf, current.AsOf);
        Assert.Equal(overview.NetWorth, current.NetWorth);
        Assert.Equal(overview.TotalDebt, current.DebtBalance);
        Assert.Equal(overview.TotalInvestments, current.InvestmentBalance);
    }

    [Fact]
    public void ValuesAreRoundedCentrallyToTwoDecimals()
    {
        var position = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 10.005m)],
            cards: [Card(1.004m)]);

        Assert.Equal(10.01m, position.Assets.CashAccounts);
        Assert.Equal(1.00m, position.Liabilities.CreditCards);
        Assert.Equal(9.01m, position.NetWorth);
    }

    [Fact]
    public void OpeningAndAdjustmentTotals_AreAuditableButNotAddedAgain()
    {
        var position = Calculate(
            accounts: [Account(FinancialAccountType.Cash, 100m)],
            accountOpeningBalances: 80m,
            accountAdjustments: 20m);

        Assert.Equal(80m, position.Assets.AccountOpeningBalances);
        Assert.Equal(20m, position.Assets.AccountAdjustments);
        Assert.Equal(100m, position.NetWorth);
    }

    private static FinancialPositionDto Calculate(
        IEnumerable<FinancialAccount>? accounts = null,
        IEnumerable<Investment>? investments = null,
        IEnumerable<Debt>? debts = null,
        IEnumerable<CreditCard>? cards = null,
        decimal goalAllocations = 0m,
        decimal accountOpeningBalances = 0m,
        decimal accountAdjustments = 0m) =>
        FinancialPositionService.CalculateCurrentSnapshot(
            Today,
            accounts ?? [],
            investments ?? [],
            debts ?? [],
            cards ?? [],
            goalAllocations,
            accountOpeningBalances,
            accountAdjustments);

    private static FinancialAccount Account(
        FinancialAccountType type,
        decimal balance,
        bool isActive = true) => new()
    {
        Type = type,
        CurrentBalance = balance,
        IsActive = isActive
    };

    private static Investment Investment(decimal value, bool isActive = true, decimal? costBasis = null) => new()
    {
        InitialAmount = costBasis ?? value,
        CurrentValue = value,
        IsActive = isActive
    };

    private static Debt Debt(decimal balance, bool isActive = true) => new()
    {
        CurrentBalance = balance,
        IsActive = isActive
    };

    private static CreditCard Card(decimal balance, bool isActive = true) => new()
    {
        CurrentBalance = balance,
        IsActive = isActive
    };
}
