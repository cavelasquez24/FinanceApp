using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;

namespace FinanceApp.UnitTests;

public class EmergencyFundRestorationTests
{
    [Fact]
    public void MonitorExample_AfterRestoring50_Leaves260Outstanding()
    {
        var restoration = NewRestoration();

        restoration.ApplyPayment(50m, new DateOnly(2026, 8, 15));

        Assert.Equal(50m, restoration.RestoredAmount);
        Assert.Equal(260m, restoration.OutstandingAmount);
        Assert.Equal(EmergencyFundRestorationStatus.Open, restoration.Status);
        Assert.Equal(new DateOnly(2026, 9, 15), restoration.NextScheduledDate);
    }

    [Fact]
    public void FinalPayment_CompletesRestorationAndKeepsAcquisitionDateSeparate()
    {
        var restoration = NewRestoration();
        restoration.RestoredAmount = 260m;
        var completedDate = new DateOnly(2026, 12, 15);

        restoration.ApplyPayment(50m, completedDate);

        Assert.Equal(0m, restoration.OutstandingAmount);
        Assert.Equal(EmergencyFundRestorationStatus.Completed, restoration.Status);
        Assert.Equal(completedDate, restoration.CompletedDate);
        Assert.Equal(new DateOnly(2026, 8, 2), restoration.AcquisitionDate);
    }

    [Fact]
    public void PaymentAboveOutstanding_IsRejected()
    {
        var restoration = NewRestoration();

        var error = Assert.Throws<DomainException>(() =>
            restoration.ApplyPayment(311m, new DateOnly(2026, 8, 15)));

        Assert.Equal("INVALID_RESTORATION_AMOUNT", error.Code);
    }

    [Fact]
    public void FundedExpense_IsCountedOnceAndWithdrawalOnlyFundsCash()
    {
        var residual = CashFlowCalculator.CalculateResidual(
            income: 0m,
            consumptionExpenses: 310m,
            savingsContributions: 0m,
            savingsWithdrawals: 310m,
            investmentContributions: 0m,
            debtPrincipalPaid: 0m);

        Assert.Equal(0m, residual);
    }

    [Fact]
    public void RestorationContribution_UsesCashButDoesNotCreateAnotherExpense()
    {
        var residual = CashFlowCalculator.CalculateResidual(
            income: 100m,
            consumptionExpenses: 0m,
            savingsContributions: 50m,
            savingsWithdrawals: 0m,
            investmentContributions: 0m,
            debtPrincipalPaid: 0m);

        Assert.Equal(50m, residual);
    }

    [Fact]
    public void ExtraPayment_KeepsMonthlyAmountAndMovesEstimatedCompletionForward()
    {
        var restoration = NewRestoration();
        Assert.Equal(new DateOnly(2027, 2, 15), restoration.EstimatedCompletionDate);

        restoration.ApplyPayment(110m, new DateOnly(2026, 8, 10));

        Assert.Equal(200m, restoration.OutstandingAmount);
        Assert.Equal(50m, restoration.ScheduledContributionAmount);
        Assert.Equal(new DateOnly(2026, 11, 15), restoration.EstimatedCompletionDate);
    }

    [Fact]
    public void EstimatedCompletion_ClampsDayForShorterMonths()
    {
        var restoration = new EmergencyFundRestoration
        {
            OriginalAmount = 200m,
            ScheduledContributionAmount = 100m,
            NextScheduledDate = new DateOnly(2027, 1, 31),
            Status = EmergencyFundRestorationStatus.Open
        };

        Assert.Equal(new DateOnly(2027, 2, 28), restoration.EstimatedCompletionDate);
    }

    private static EmergencyFundRestoration NewRestoration() => new()
    {
        Description = "Monitor",
        AcquisitionDate = new DateOnly(2026, 8, 2),
        OriginalAmount = 310m,
        RestoredAmount = 0m,
        TargetRestorationDate = new DateOnly(2026, 12, 31),
        ScheduledContributionAmount = 50m,
        NextScheduledDate = new DateOnly(2026, 8, 15),
        Status = EmergencyFundRestorationStatus.Open
    };
}
