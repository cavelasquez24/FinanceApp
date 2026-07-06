using FinanceApp.Application.DTOs.Dashboard;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IIncomeRepository _incomeRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IInvestmentRepository _investmentRepository;
    private readonly ISavingsGoalRepository _savingsGoalRepository;

    // Nombres de los meses en español para las etiquetas del gráfico
    private static readonly string[] MonthNames =
    {
        "Ene", "Feb", "Mar", "Abr", "May", "Jun",
        "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"
    };

    public DashboardService(
        IIncomeRepository incomeRepository,
        IExpenseRepository expenseRepository,
        IInvestmentRepository investmentRepository,
        ISavingsGoalRepository savingsGoalRepository)
    {
        _incomeRepository = incomeRepository;
        _expenseRepository = expenseRepository;
        _investmentRepository = investmentRepository;
        _savingsGoalRepository = savingsGoalRepository;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(
    Guid userId,
    int month,
    int year,
    CancellationToken cancellationToken = default)
    {
        // Calculamos el mes anterior para comparación
        var prevMonth = month == 1 ? 12 : month - 1;
        var prevYear = month == 1 ? year - 1 : year;

        // ✅ Ejecutamos secuencialmente — EF Core no es thread-safe
        var totalIncome = await _incomeRepository
            .GetTotalByUserAndPeriodAsync(userId, month, year, cancellationToken);

        var totalExpenses = await _expenseRepository
            .GetTotalByUserAndPeriodAsync(userId, month, year, cancellationToken);

        var prevIncome = await _incomeRepository
            .GetTotalByUserAndPeriodAsync(userId, prevMonth, prevYear, cancellationToken);

        var prevExpenses = await _expenseRepository
            .GetTotalByUserAndPeriodAsync(userId, prevMonth, prevYear, cancellationToken);

        var totalInvestments = await _investmentRepository
            .GetTotalCurrentValueAsync(userId, cancellationToken);

        var totalSavings = await _savingsGoalRepository
            .GetTotalSavedAsync(userId, cancellationToken);

        var netSavings = totalIncome - totalExpenses;
        var prevNetSavings = prevIncome - prevExpenses;
        var savingsRate = totalIncome > 0
            ? Math.Round(netSavings / totalIncome * 100, 2)
            : 0;

        var netWorth = netSavings + totalInvestments + totalSavings;

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
            NetSavings = netSavings,
            SavingsRate = savingsRate,
            TotalInvestments = totalInvestments,
            TotalSavingsGoals = totalSavings,
            NetWorth = netWorth,
            PreviousMonth = new PreviousMonthDto
            {
                TotalIncome = prevIncome,
                TotalExpenses = prevExpenses,
                NetSavings = prevNetSavings
            },
            Changes = new ChangesDto
            {
                IncomeChange = CalculateChange(prevIncome, totalIncome),
                ExpensesChange = CalculateChange(prevExpenses, totalExpenses),
                SavingsChange = CalculateChange(prevNetSavings, netSavings)
            }
        };
    }

    public async Task<MonthlyTrendDto> GetMonthlyTrendAsync(
        Guid userId,
        int months,
        CancellationToken cancellationToken = default)
    {
        var trend = new MonthlyTrendDto();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Generamos los últimos N meses hacia atrás desde hoy
        for (int i = months - 1; i >= 0; i--)
        {
            // Calculamos el mes correspondiente
            var date = today.AddMonths(-i);
            var month = date.Month;
            var year = date.Year;

            var income = await _incomeRepository
                .GetTotalByUserAndPeriodAsync(userId, month, year, cancellationToken);
            var expenses = await _expenseRepository
                .GetTotalByUserAndPeriodAsync(userId, month, year, cancellationToken);

            trend.Labels.Add($"{MonthNames[month - 1]} {year.ToString()[2..]}");
            trend.Income.Add(income);
            trend.Expenses.Add(expenses);
            trend.Savings.Add(income - expenses);
        }

        return trend;
    }

    public async Task<ExpenseByCategoryChartDto> GetExpensesByCategoryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var categoryData = await _expenseRepository
            .GetByCategoryAsync(userId, month, year, cancellationToken);

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
                Percentage = totalAmount > 0
                    ? Math.Round(c.Total / totalAmount * 100, 2)
                    : 0
            }).ToList()
        };
    }

    /// <summary>
    /// Calcula el porcentaje de cambio entre dos valores.
    /// Si el valor anterior es 0, retorna 100 (crecimiento total).
    /// </summary>
    private static decimal CalculateChange(decimal previous, decimal current)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round((current - previous) / Math.Abs(previous) * 100, 2);
    }
}