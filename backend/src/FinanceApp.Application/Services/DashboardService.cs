using FinanceApp.Application.DTOs.Dashboard;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class DashboardService : IDashboardService
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
    private readonly IEmergencyFundRestorationRepository _restorationRepository;

    private static readonly string[] MonthNames =
    {
        "Ene", "Feb", "Mar", "Abr", "May", "Jun",
        "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"
    };

    public DashboardService(
        IIncomeRepository incomeRepository,
        IExpenseRepository expenseRepository,
        IReimbursementRepository reimbursementRepository,
        IInvestmentRepository investmentRepository,
        ISavingsGoalRepository savingsGoalRepository,
        IDebtRepository debtRepository,
        ICreditCardRepository creditCardRepository,
        IUserRepository userRepository,
        IFinancialPositionService financialPositionService,
        IEmergencyFundRestorationRepository restorationRepository)
    {
        _incomeRepository = incomeRepository;
        _expenseRepository = expenseRepository;
        _reimbursementRepository = reimbursementRepository;
        _investmentRepository = investmentRepository;
        _savingsGoalRepository = savingsGoalRepository;
        _debtRepository = debtRepository;
        _userRepository = userRepository;
        _creditCardRepository = creditCardRepository;
        _financialPositionService = financialPositionService;
        _restorationRepository = restorationRepository;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(
        Guid userId, int month, int year,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var paydayDay = user?.PaydayDay;

        var prevMonth = month == 1 ? 12 : month - 1;
        var prevYear = month == 1 ? year - 1 : year;

        var (start, end) = GetCycleRange(month, year, paydayDay);
        var (prevStart, prevEnd) = GetCycleRange(prevMonth, prevYear, paydayDay);

        var totalIncome = await _incomeRepository.GetTotalByCycleAsync(userId, start, end, cancellationToken);
        var totalExpenses = await _expenseRepository.GetTotalByDateRangeAsync(userId, start, end, cancellationToken);
        var reimbursements = await _reimbursementRepository.GetTotalByDateRangeAsync(userId, start, end, cancellationToken);
        var prevIncome = await _incomeRepository.GetTotalByCycleAsync(userId, prevStart, prevEnd, cancellationToken);
        var prevExpenses = await _expenseRepository.GetTotalByDateRangeAsync(userId, prevStart, prevEnd, cancellationToken);
        var prevReimbursements = await _reimbursementRepository.GetTotalByDateRangeAsync(userId, prevStart, prevEnd, cancellationToken);
        var netPersonalExpenses = totalExpenses - reimbursements;
        var prevNetPersonalExpenses = prevExpenses - prevReimbursements;

        var totalSavings = await _savingsGoalRepository.GetTotalSavedAsync(userId, cancellationToken);

        var financialPosition = await _financialPositionService.GetAsync(userId, null, cancellationToken);
        var pendingRestoration = await _restorationRepository.GetTotalOutstandingAsync(userId, cancellationToken);

        // Pagos de deuda dentro del ciclo — métrica de cash flow, separada de Expenses
        var cardPrincipalPaid = await _creditCardRepository.GetTotalPrincipalPaidByDateRangeAsync(userId, start, end, cancellationToken);
        var prevCardPrincipalPaid = await _creditCardRepository.GetTotalPrincipalPaidByDateRangeAsync(userId, prevStart, prevEnd, cancellationToken);
        var totalDebtPayments = await _debtRepository.GetTotalPaymentsByDateRangeAsync(userId, start, end, cancellationToken)
            + cardPrincipalPaid;
        var prevDebtPayments = await _debtRepository.GetTotalPaymentsByDateRangeAsync(userId, prevStart, prevEnd, cancellationToken)
            + prevCardPrincipalPaid;
        // Las metas registran asignaciones manuales; no son entradas ni salidas físicas.
        const decimal savingsContributions = 0m;
        const decimal prevSavingsContributions = 0m;
        const decimal savingsWithdrawals = 0m;
        const decimal prevSavingsWithdrawals = 0m;
        var investmentContributions = await _investmentRepository.GetTotalContributionsByDateRangeAsync(userId, start, end, cancellationToken);
        var prevInvestmentContributions = await _investmentRepository.GetTotalContributionsByDateRangeAsync(userId, prevStart, prevEnd, cancellationToken);
        var debtPrincipalPaid = await _debtRepository.GetTotalPrincipalPaidByDateRangeAsync(userId, start, end, cancellationToken)
            + cardPrincipalPaid;
        var prevDebtPrincipalPaid = await _debtRepository.GetTotalPrincipalPaidByDateRangeAsync(userId, prevStart, prevEnd, cancellationToken)
            + prevCardPrincipalPaid;

        var netSavings = CashFlowCalculator.CalculateResidual(
            totalIncome,
            netPersonalExpenses,
            savingsContributions,
            savingsWithdrawals,
            investmentContributions,
            debtPrincipalPaid);
        var prevNetSavings = CashFlowCalculator.CalculateResidual(
            prevIncome,
            prevNetPersonalExpenses,
            prevSavingsContributions,
            prevSavingsWithdrawals,
            prevInvestmentContributions,
            prevDebtPrincipalPaid);
        var savingsRate = totalIncome > 0 ? Math.Round(netSavings / totalIncome * 100, 2) : 0;


        return new DashboardOverviewDto
        {
            Period = new PeriodDto
            {
                Month = month,
                Year = year,
                Label = $"{MonthNames[month - 1]} {year}"
            },
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            ReimbursementsReceived = reimbursements,
            NetPersonalExpenses = netPersonalExpenses,
            NetSavings = netSavings,
            SavingsRate = savingsRate,
            TotalDebtPayments = totalDebtPayments,
            TotalSavingsGoals = totalSavings,
            FinancialPosition = financialPosition,
            PendingEmergencyFundRestoration = pendingRestoration,
            PreviousMonth = new PreviousMonthDto
            {
                TotalIncome = prevIncome,
                TotalExpenses = prevExpenses,
                NetSavings = prevNetSavings,
                TotalDebtPayments = prevDebtPayments
            },
            Changes = new ChangesDto
            {
                IncomeChange = CalculateChange(prevIncome, totalIncome),
                ExpensesChange = CalculateChange(prevExpenses, totalExpenses),
                SavingsChange = CalculateChange(prevNetSavings, netSavings),
                DebtPaymentsChange = CalculateChange(prevDebtPayments, totalDebtPayments)
            }
        };
    }

    public async Task<MonthlyTrendDto> GetMonthlyTrendAsync(
        Guid userId, int months, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var paydayDay = user?.PaydayDay;

        var trend = new MonthlyTrendDto();
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (int i = months - 1; i >= 0; i--)
        {
            var date = today.AddMonths(-i);
            var month = date.Month;
            var year = date.Year;

            var (start, end) = GetCycleRange(month, year, paydayDay);

            var income = await _incomeRepository.GetTotalByCycleAsync(userId, start, end, cancellationToken);
            var expenses = await _expenseRepository.GetTotalByDateRangeAsync(userId, start, end, cancellationToken);
            var reimbursements = await _reimbursementRepository.GetTotalByDateRangeAsync(userId, start, end, cancellationToken);
            const decimal savings = 0m;
            const decimal withdrawals = 0m;
            var investments = await _investmentRepository.GetTotalContributionsByDateRangeAsync(userId, start, end, cancellationToken);
            var debtPrincipal = await _debtRepository.GetTotalPrincipalPaidByDateRangeAsync(userId, start, end, cancellationToken);
            var residual = CashFlowCalculator.CalculateResidual(
                income,
                expenses - reimbursements,
                savings,
                withdrawals,
                investments,
                debtPrincipal);

            trend.Labels.Add($"{MonthNames[month - 1]} {year.ToString()[2..]}");
            trend.Income.Add(income);
            trend.Expenses.Add(expenses);
            trend.Residual.Add(residual);
        }

        return trend;
    }

    public async Task<ExpenseByCategoryChartDto> GetExpensesByCategoryAsync(
        Guid userId, int month, int year,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var (start, end) = GetCycleRange(month, year, user?.PaydayDay);

        var categoryData = await _expenseRepository
            .GetByCategoryByDateRangeAsync(userId, start, end, cancellationToken);

        var categoryList = categoryData.ToList();
        var totalAmount = categoryList.Sum(c => c.Total);

        return new ExpenseByCategoryChartDto
        {
            TotalAmount = totalAmount,
            Categories = categoryList.Select(c => new CategoryChartItemDto
            {
                CategoryName = c.CategoryName,
                CategoryColor = c.CategoryColor,
                Amount = c.Total,
                Percentage = totalAmount > 0 ? Math.Round(c.Total / totalAmount * 100, 2) : 0
            }).ToList()
        };
    }

    public async Task<CashFlowStatementDto> GetCashFlowStatementAsync(
        Guid userId, int month, int year,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var (start, end) = GetCycleRange(month, year, user?.PaydayDay);

        var income = await _incomeRepository.GetTotalByCycleAsync(userId, start, end, cancellationToken);
        var consumptionExpenses = await _expenseRepository.GetTotalByDateRangeAsync(userId, start, end, cancellationToken);
        var reimbursements = await _reimbursementRepository.GetTotalByDateRangeAsync(userId, start, end, cancellationToken);
        var netPersonalExpenses = consumptionExpenses - reimbursements;
        // Se exponen como cero en flujo de caja: son progreso interno de metas.
        const decimal savingsContributions = 0m;
        const decimal savingsWithdrawals = 0m;
        var investmentContributions = await _investmentRepository.GetTotalContributionsByDateRangeAsync(userId, start, end, cancellationToken);
        var debtPrincipalPaid = await _debtRepository.GetTotalPrincipalPaidByDateRangeAsync(userId, start, end, cancellationToken);
        const decimal restorationContributions = 0m;
        const decimal newSavingsContributions = 0m;

        var cashFlowResidual = CashFlowCalculator.CalculateResidual(
            income,
            netPersonalExpenses,
            savingsContributions,
            savingsWithdrawals,
            investmentContributions,
            debtPrincipalPaid);

        var wealthBuilding = newSavingsContributions - savingsWithdrawals + investmentContributions + debtPrincipalPaid;

        return new CashFlowStatementDto
        {
            Income = income,
            ConsumptionExpenses = consumptionExpenses,
            ReimbursementsReceived = reimbursements,
            NetPersonalExpenses = netPersonalExpenses,
            SavingsContributions = savingsContributions,
            InvestmentContributions = investmentContributions,
            SavingsWithdrawals = savingsWithdrawals,
            DebtPrincipalPaid = debtPrincipalPaid,
            RestorationContributions = restorationContributions,
            NewSavingsContributions = newSavingsContributions,
            CashFlowResidual = cashFlowResidual,
            ConsumptionRate = income > 0 ? Math.Round(netPersonalExpenses / income * 100, 2) : 0,
            WealthBuildingRate = income > 0 ? Math.Round(wealthBuilding / income * 100, 2) : 0
        };
    }

    private static decimal CalculateChange(decimal previous, decimal current)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round((current - previous) / Math.Abs(previous) * 100, 2);
    }

    private static (DateOnly Start, DateOnly End) GetCycleRange(int month, int year, int? paydayDay)
    {
        if (paydayDay is null)
        {
            var start = new DateOnly(year, month, 1);
            var end = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            return (start, end);
        }

        var day = Math.Min(paydayDay.Value, DateTime.DaysInMonth(year, month));
        var cycleStart = new DateOnly(year, month, day);

        var nextMonth = month == 12 ? 1 : month + 1;
        var nextYear = month == 12 ? year + 1 : year;
        var nextDay = Math.Min(paydayDay.Value, DateTime.DaysInMonth(nextYear, nextMonth));
        var cycleEnd = new DateOnly(nextYear, nextMonth, nextDay).AddDays(-1);

        return (cycleStart, cycleEnd);
    }
}
