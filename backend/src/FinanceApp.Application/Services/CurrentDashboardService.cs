using FinanceApp.Application.DTOs.Dashboard;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class CurrentDashboardService : ICurrentDashboardService
{
    private readonly IIncomeRepository _incomeRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IReimbursementRepository _reimbursementRepository;
    private readonly IInvestmentRepository _investmentRepository;
    private readonly ISavingsGoalRepository _savingsGoalRepository;
    private readonly IDebtRepository _debtRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFinancialPositionService _financialPositionService;
    private readonly IBudgetService _budgetService;
    private readonly IBusinessDateProvider _businessDateProvider;

    public CurrentDashboardService(
        IIncomeRepository incomeRepository,
        IExpenseRepository expenseRepository,
        IReimbursementRepository reimbursementRepository,
        IInvestmentRepository investmentRepository,
        ISavingsGoalRepository savingsGoalRepository,
        IDebtRepository debtRepository,
        ICreditCardRepository creditCardRepository,
        IUserRepository userRepository,
        IFinancialPositionService financialPositionService,
        IBudgetService budgetService,
        IBusinessDateProvider businessDateProvider)
    {
        _incomeRepository = incomeRepository;
        _expenseRepository = expenseRepository;
        _reimbursementRepository = reimbursementRepository;
        _investmentRepository = investmentRepository;
        _savingsGoalRepository = savingsGoalRepository;
        _debtRepository = debtRepository;
        _creditCardRepository = creditCardRepository;
        _userRepository = userRepository;
        _financialPositionService = financialPositionService;
        _budgetService = budgetService;
        _businessDateProvider = businessDateProvider;
    }

    public async Task<CurrentDashboardDto> GetAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var today = _businessDateProvider.Today;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var (cycleMonth, cycleYear) = GetCurrentCycle(today, user?.PaydayDay);
        var (start, end) = GetCycleRange(cycleMonth, cycleYear, user?.PaydayDay);

        var financialPosition = await _financialPositionService.GetAsync(
            userId, today, cancellationToken);

        var income = await _incomeRepository.GetTotalByCycleAsync(
            userId, start, end, cancellationToken);
        var expenses = await _expenseRepository.GetTotalByDateRangeAsync(
            userId, start, end, cancellationToken);
        var reimbursements = await _reimbursementRepository.GetTotalByDateRangeAsync(userId, start, end, cancellationToken);
        var netExpenses = expenses - reimbursements;
        var investments = await _investmentRepository.GetTotalContributionsByDateRangeAsync(
            userId, start, end, cancellationToken);
        var debtPayments = await _debtRepository.GetTotalPaymentsByDateRangeAsync(
            userId, start, end, cancellationToken)
            + await _creditCardRepository.GetTotalPrincipalPaidByDateRangeAsync(
                userId, start, end, cancellationToken);

        // Flujo de caja del ciclo: movimientos reales, no saldos de apertura ni transferencias.
        // Los reembolsos se mantienen separados de ingresos ganados, aunque restauran el efectivo.
        // Las metas son seguimiento manual y no forman parte del flujo de caja.
        var cashFlow = income + reimbursements - expenses - investments - debtPayments;
        var budget = await _budgetService.GetByPeriodAsync(userId, cycleMonth, cycleYear, cancellationToken);
        var budgetAvailable = budget is null
            ? 0m
            : (await _budgetService.GetStatusAsync(budget.Id, userId, cancellationToken)).TotalRemaining;
        var daysRemaining = Math.Max(0, end.DayNumber - today.DayNumber + 1);

        return new CurrentDashboardDto
        {
            CycleStart = start,
            CycleEnd = end,
            CycleLabel = $"{start:dd MMM} – {end:dd MMM}",
            FinancialPosition = financialPosition,
            CycleIncome = income,
            CycleExpenses = expenses,
            CycleReimbursements = reimbursements,
            CycleNetExpenses = netExpenses,
            CycleSavings = 0m,
            CycleInvestments = investments,
            CycleDebtPayments = debtPayments,
            CycleCashFlow = cashFlow,
            BudgetAvailable = budgetAvailable,
            // Compatibilidad temporal: ahora significa disponible presupuestario, no saldo físico.
            CycleAvailable = budgetAvailable,
            DaysRemaining = daysRemaining,
            SuggestedDailyAvailable = daysRemaining > 0
                ? Math.Round(budgetAvailable / daysRemaining, 2)
                : budgetAvailable
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
