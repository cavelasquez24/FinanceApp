using FinanceApp.Application.DTOs.Dashboard;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class CurrentDashboardService : ICurrentDashboardService
{
    private readonly IIncomeRepository _incomeRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IInvestmentRepository _investmentRepository;
    private readonly ISavingsGoalRepository _savingsGoalRepository;
    private readonly IDebtRepository _debtRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFinancialAccountService _accountService;

    public CurrentDashboardService(
        IIncomeRepository incomeRepository,
        IExpenseRepository expenseRepository,
        IInvestmentRepository investmentRepository,
        ISavingsGoalRepository savingsGoalRepository,
        IDebtRepository debtRepository,
        IUserRepository userRepository,
        IFinancialAccountService accountService)
    {
        _incomeRepository = incomeRepository;
        _expenseRepository = expenseRepository;
        _investmentRepository = investmentRepository;
        _savingsGoalRepository = savingsGoalRepository;
        _debtRepository = debtRepository;
        _userRepository = userRepository;
        _accountService = accountService;
    }

    public async Task<CurrentDashboardDto> GetAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var (cycleMonth, cycleYear) = GetCurrentCycle(today, user?.PaydayDay);
        var (start, end) = GetCycleRange(cycleMonth, cycleYear, user?.PaydayDay);

        var accounts = await _accountService.GetAllAsync(userId, cancellationToken);
        var cash = accounts.Where(a => a.IsActive && a.Type == "cash")
            .Sum(a => a.CurrentBalance);
        var savingsBalance = accounts.Where(a => a.IsActive && a.Type == "savings")
            .Sum(a => a.CurrentBalance);
        var investmentBalance = accounts.Where(a => a.IsActive && a.Type == "investment")
            .Sum(a => a.CurrentBalance);

        var income = await _incomeRepository.GetTotalByCycleAsync(
            userId, start, end, cancellationToken);
        var expenses = await _expenseRepository.GetTotalByDateRangeAsync(
            userId, start, end, cancellationToken);
        var savings = await _savingsGoalRepository.GetTotalContributionsByDateRangeAsync(
            userId, start, end, cancellationToken);
        var savingsWithdrawals =
            await _savingsGoalRepository.GetTotalCashFlowWithdrawalsByDateRangeAsync(
                userId, start, end, cancellationToken);
        var investments = await _investmentRepository.GetTotalContributionsByDateRangeAsync(
            userId, start, end, cancellationToken);
        var debtPayments = await _debtRepository.GetTotalPaymentsByDateRangeAsync(
            userId, start, end, cancellationToken);
        var debt = await _debtRepository.GetTotalCurrentBalanceAsync(
            userId, cancellationToken);

        var available = income + savingsWithdrawals - expenses
            - savings - investments - debtPayments;
        var daysRemaining = Math.Max(0, end.DayNumber - today.DayNumber + 1);

        return new CurrentDashboardDto
        {
            AsOf = today,
            CycleStart = start,
            CycleEnd = end,
            CycleLabel = $"{start:dd MMM} – {end:dd MMM}",
            CashBalance = cash,
            SavingsBalance = savingsBalance,
            InvestmentBalance = investmentBalance,
            DebtBalance = debt,
            NetWorth = cash + savingsBalance + investmentBalance - debt,
            CycleIncome = income,
            CycleExpenses = expenses,
            CycleSavings = savings,
            CycleInvestments = investments,
            CycleDebtPayments = debtPayments,
            CycleAvailable = available,
            DaysRemaining = daysRemaining,
            SuggestedDailyAvailable = daysRemaining > 0
                ? Math.Round(available / daysRemaining, 2)
                : available
        };
    }

    private static (int Month, int Year) GetCurrentCycle(DateOnly today, int? paydayDay)
    {
        if (paydayDay is null) return (today.Month, today.Year);
        var day = Math.Min(paydayDay.Value,
            DateTime.DaysInMonth(today.Year, today.Month));
        if (today.Day >= day) return (today.Month, today.Year);
        var previous = today.AddMonths(-1);
        return (previous.Month, previous.Year);
    }

    private static (DateOnly Start, DateOnly End) GetCycleRange(
        int month, int year, int? paydayDay)
    {
        if (paydayDay is null)
            return (
                new DateOnly(year, month, 1),
                new DateOnly(year, month, DateTime.DaysInMonth(year, month)));

        var day = Math.Min(paydayDay.Value, DateTime.DaysInMonth(year, month));
        var start = new DateOnly(year, month, day);
        var next = start.AddMonths(1);
        var nextDay = Math.Min(paydayDay.Value,
            DateTime.DaysInMonth(next.Year, next.Month));
        return (start, new DateOnly(next.Year, next.Month, nextDay).AddDays(-1));
    }
}
