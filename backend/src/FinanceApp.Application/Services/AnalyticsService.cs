using FinanceApp.Application.DTOs.Analytics;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly INetWorthSnapshotRepository _snapshotRepo;
    private readonly IFinancialAccountRepository _accountRepo;
    private readonly IInvestmentRepository _investmentRepo;
    private readonly IDebtRepository _debtRepo;
    private readonly ICreditCardRepository _cardRepo;
    private readonly ISavingsGoalRepository _savingsRepo;
    private readonly IIncomeRepository _incomeRepo;
    private readonly IExpenseRepository _expenseRepo;
    private readonly IBudgetRepository _budgetRepo;

    public AnalyticsService(
        INetWorthSnapshotRepository snapshotRepo,
        IFinancialAccountRepository accountRepo,
        IInvestmentRepository investmentRepo,
        IDebtRepository debtRepo,
        ICreditCardRepository cardRepo,
        ISavingsGoalRepository savingsRepo,
        IIncomeRepository incomeRepo,
        IExpenseRepository expenseRepo,
        IBudgetRepository budgetRepo)
    {
        _snapshotRepo = snapshotRepo;
        _accountRepo = accountRepo;
        _investmentRepo = investmentRepo;
        _debtRepo = debtRepo;
        _cardRepo = cardRepo;
        _savingsRepo = savingsRepo;
        _incomeRepo = incomeRepo;
        _expenseRepo = expenseRepo;
        _budgetRepo = budgetRepo;
    }

    // ── 1. Net Worth Timeline ─────────────────────────────────────────────

    public async Task<NetWorthTimelineDto> GetNetWorthTimelineAsync(
        Guid userId, int months, CancellationToken ct = default)
    {
        await EnsureCurrentSnapshotAsync(userId, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));
        var snapshots = await _snapshotRepo.GetRangeAsync(userId, from, today, ct);

        var labels = new List<string>();
        var netWorthSeries = new List<decimal>();
        var assetsSeries = new List<decimal>();
        var liabilitiesSeries = new List<decimal>();

        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-i);
            var snap = snapshots.FirstOrDefault(s => s.SnapshotDate == monthStart);

            labels.Add(monthStart.ToString("MMM yy", System.Globalization.CultureInfo.GetCultureInfo("es-EC")));
            netWorthSeries.Add(snap?.NetWorth ?? 0m);
            assetsSeries.Add(snap?.TotalAssets ?? 0m);
            liabilitiesSeries.Add(snap?.TotalLiabilities ?? 0m);
        }

        var first = netWorthSeries.FirstOrDefault(v => v != 0);
        var last = netWorthSeries.LastOrDefault();
        var change = last - first;
        var changePct = first != 0 ? Math.Round(change / first * 100, 2) : 0m;

        return new NetWorthTimelineDto
        {
            Labels = labels,
            NetWorth = netWorthSeries,
            TotalAssets = assetsSeries,
            TotalLiabilities = liabilitiesSeries,
            NetWorthChange = Round(change),
            NetWorthChangePct = changePct
        };
    }

    // ── 2. Financial Health Score ─────────────────────────────────────────

    public async Task<FinancialHealthScoreDto> GetFinancialHealthScoreAsync(
        Guid userId, int month, int year, CancellationToken ct = default)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var income = await _incomeRepo.GetTotalByUserAndPeriodAsync(userId, month, year, ct);
        var expenses = await _expenseRepo.GetTotalByUserAndPeriodAsync(userId, month, year, ct);
        var debtPayments = await _debtRepo.GetTotalPaymentsByDateRangeAsync(userId, start, end, ct);
        var savingsContribs = await _savingsRepo.GetTotalContributionsByDateRangeAsync(userId, start, end, ct);
        var investContribs = await GetInvestmentContributionsByPeriodAsync(userId, start, end, ct);

        var emergencyFund = await GetEmergencyFundBalanceAsync(userId, ct);
        var avgMonthlyExpenses = await GetAvgMonthlyExpensesAsync(userId, 3, ct);
        var budget = await _budgetRepo.GetByUserAndPeriodAsync(userId, month, year, ct);

        var netSavings = income - expenses - debtPayments;

        // Savings Rate (25%)
        var savingsRateValue = income > 0 ? Math.Round(netSavings / income * 100, 2) : 0m;
        var savingsRateScore = ScoreFromRate(savingsRateValue, 20m, ascending: true);
        var savingsRateStatus = StatusFromScore(savingsRateScore);

        // Debt-to-Income (20%)
        var dtiValue = income > 0 ? Math.Round(debtPayments / income * 100, 2) : 0m;
        var dtiScore = ScoreFromRate(dtiValue, 35m, ascending: false);
        var dtiStatus = StatusFromScore(dtiScore);

        // Emergency Fund Coverage (20%)
        var efMonths = avgMonthlyExpenses > 0 ? Math.Round(emergencyFund / avgMonthlyExpenses, 2) : 0m;
        var efScore = ScoreFromRate(efMonths, 3m, ascending: true, cap: 6m);
        var efStatus = StatusFromScore(efScore);

        // Expense Ratio (15%)
        var expRatioValue = income > 0 ? Math.Round(expenses / income * 100, 2) : 0m;
        var expRatioScore = ScoreFromRate(expRatioValue, 50m, ascending: false);
        var expRatioStatus = StatusFromScore(expRatioScore);

        // Budget Adherence (10%)
        decimal budgetAdherenceValue;
        int budgetAdherenceScore;
        if (budget is not null && budget.TotalLimit.HasValue && budget.TotalLimit > 0)
        {
            budgetAdherenceValue = Math.Round(expenses / budget.TotalLimit.Value * 100, 2);
            budgetAdherenceScore = budgetAdherenceValue <= 100m ? 100 : (int)Math.Max(0, 100 - (budgetAdherenceValue - 100));
        }
        else
        {
            budgetAdherenceValue = 0m;
            budgetAdherenceScore = 50;
        }
        var budgetAdherenceStatus = StatusFromScore(budgetAdherenceScore);

        // Investment Rate (10%)
        var investRateValue = income > 0 ? Math.Round(investContribs / income * 100, 2) : 0m;
        var investRateScore = ScoreFromRate(investRateValue, 10m, ascending: true);
        var investRateStatus = StatusFromScore(investRateScore);

        var weightedScore = (int)Math.Round(
            savingsRateScore * 0.25m +
            dtiScore * 0.20m +
            efScore * 0.20m +
            expRatioScore * 0.15m +
            budgetAdherenceScore * 0.10m +
            investRateScore * 0.10m);

        var recommendations = BuildRecommendations(
            savingsRateScore, dtiScore, efScore, expRatioScore, budgetAdherenceScore, investRateScore,
            savingsRateValue, dtiValue, efMonths, expRatioValue, budgetAdherenceValue, investRateValue);

        return new FinancialHealthScoreDto
        {
            Score = weightedScore,
            Grade = GradeFromScore(weightedScore),
            Components = new HealthScoreComponents
            {
                SavingsRate = new ScoreComponent
                {
                    Score = savingsRateScore,
                    Value = savingsRateValue,
                    Benchmark = 20m,
                    Label = "Tasa de ahorro",
                    Status = savingsRateStatus
                },
                DebtToIncome = new ScoreComponent
                {
                    Score = dtiScore,
                    Value = dtiValue,
                    Benchmark = 35m,
                    Label = "Deuda / Ingreso",
                    Status = dtiStatus
                },
                EmergencyFundCoverage = new ScoreComponent
                {
                    Score = efScore,
                    Value = efMonths,
                    Benchmark = 3m,
                    Label = "Fondo de emergencia",
                    Status = efStatus
                },
                ExpenseRatio = new ScoreComponent
                {
                    Score = expRatioScore,
                    Value = expRatioValue,
                    Benchmark = 50m,
                    Label = "Ratio de gastos",
                    Status = expRatioStatus
                },
                BudgetAdherence = new ScoreComponent
                {
                    Score = budgetAdherenceScore,
                    Value = budgetAdherenceValue,
                    Benchmark = 100m,
                    Label = "Adherencia al presupuesto",
                    Status = budgetAdherenceStatus
                },
                InvestmentRate = new ScoreComponent
                {
                    Score = investRateScore,
                    Value = investRateValue,
                    Benchmark = 10m,
                    Label = "Tasa de inversión",
                    Status = investRateStatus
                }
            },
            Recommendations = recommendations
        };
    }

    // ── 3. Expense Intelligence ───────────────────────────────────────────

    public async Task<ExpenseIntelligenceDto> GetExpenseIntelligenceAsync(
        Guid userId, int month, int year, CancellationToken ct = default)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var prevStart = start.AddMonths(-1);
        var prevEnd = start.AddDays(-1);

        var topMerchantsRaw = await _expenseRepo.GetTopMerchantsByDateRangeAsync(userId, start, end, 5, ct);
        var recurring = await _expenseRepo.GetRecurringByUserAsync(userId, ct);
        var currentByCategory = await _expenseRepo.GetByCategoryByDateRangeAsync(userId, start, end, ct);
        var prevByCategory = await _expenseRepo.GetByCategoryByDateRangeAsync(userId, prevStart, prevEnd, ct);

        var topMerchants = topMerchantsRaw.Select(m => new TopMerchantDto
        {
            Merchant = m.Merchant,
            TotalAmount = m.Total,
            TransactionCount = m.Count,
            CategoryName = m.CategoryName
        }).ToList();

        var recurringDtos = recurring.Select(e => new RecurringExpenseDto
        {
            ExpenseId = e.Id,
            Description = e.Description ?? e.Merchant ?? "(sin nombre)",
            Amount = e.Amount,
            RecurrenceType = e.RecurrenceType?.ToString() ?? "Monthly",
            CategoryName = e.Category?.Name ?? "",
            AnnualImpact = AnnualImpact(e.Amount, e.RecurrenceType)
        }).ToList();

        var prevDict = prevByCategory.ToDictionary(x => x.CategoryName, x => x.Total);
        var drift = currentByCategory.Select(c =>
        {
            prevDict.TryGetValue(c.CategoryName, out var prev);
            var driftAmt = c.Total - prev;
            var driftPct = prev > 0 ? Math.Round(driftAmt / prev * 100, 2) : 0m;
            return new CategoryDriftDto
            {
                CategoryName = c.CategoryName,
                CategoryColor = c.CategoryColor,
                CurrentAmount = c.Total,
                PreviousAmount = prev,
                DriftAmount = Round(driftAmt),
                DriftPct = driftPct
            };
        }).OrderByDescending(d => Math.Abs(d.DriftAmount)).ToList();

        return new ExpenseIntelligenceDto
        {
            TopMerchants = topMerchants,
            RecurringExpenses = recurringDtos,
            CategoryDrift = drift
        };
    }

    // ── 4. Debt Projection ────────────────────────────────────────────────

    public async Task<DebtProjectionDto> GetDebtProjectionAsync(Guid userId, CancellationToken ct = default)
    {
        var debts = (await _debtRepo.GetByUserIdAsync(userId, ct))
            .Where(d => d.IsActive && d.CurrentBalance > 0)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lines = new List<DebtLineProjectionDto>();

        foreach (var debt in debts)
        {
            var avgPayment = await _debtRepo.GetAvgMonthlyPaymentAsync(debt.Id, 3, ct);
            int? estMonths = avgPayment > 0 ? (int)Math.Ceiling((double)(debt.CurrentBalance / avgPayment)) : null;
            DateOnly? estDate = estMonths.HasValue ? today.AddMonths(estMonths.Value) : null;

            lines.Add(new DebtLineProjectionDto
            {
                DebtId = debt.Id,
                Name = debt.Name,
                CurrentBalance = debt.CurrentBalance,
                AvgMonthlyPayment = Round(avgPayment),
                EstimatedPayoffMonths = estMonths,
                EstimatedPayoffDate = estDate
            });
        }

        var totalBalance = lines.Sum(l => l.CurrentBalance);
        var totalAvgPayment = lines.Sum(l => l.AvgMonthlyPayment);
        int? totalMonths = totalAvgPayment > 0
            ? (int)Math.Ceiling((double)(totalBalance / totalAvgPayment))
            : null;

        return new DebtProjectionDto
        {
            TotalOutstanding = Round(totalBalance),
            AvgMonthlyPayment = Round(totalAvgPayment),
            EstimatedPayoffMonths = totalMonths,
            EstimatedPayoffDate = totalMonths.HasValue ? today.AddMonths(totalMonths.Value) : null,
            ByDebt = lines
        };
    }

    // ── 5. Savings Goal ETA ───────────────────────────────────────────────

    public async Task<IReadOnlyList<SavingsGoalEtaDto>> GetSavingsGoalEtaAsync(
        Guid userId, CancellationToken ct = default)
    {
        var goals = (await _savingsRepo.GetByUserIdAsync(userId, ct))
            .Where(g => !g.IsCompleted)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = new List<SavingsGoalEtaDto>();

        foreach (var goal in goals)
        {
            var avgContrib = await _savingsRepo.GetAvgMonthlyContributionAsync(goal.Id, 3, ct);
            var remaining = goal.RemainingAmount;
            int? estMonths = avgContrib > 0 ? (int)Math.Ceiling((double)(remaining / avgContrib)) : null;
            DateOnly? estDate = estMonths.HasValue ? today.AddMonths(estMonths.Value) : null;

            bool isOnTrack = goal.TargetDate.HasValue && estDate.HasValue
                ? estDate.Value <= goal.TargetDate.Value
                : estMonths.HasValue;

            result.Add(new SavingsGoalEtaDto
            {
                GoalId = goal.Id,
                Name = goal.Name,
                CurrentAmount = goal.CurrentAmount,
                TargetAmount = goal.TargetAmount,
                Remaining = remaining,
                ProgressPct = Round(goal.ProgressPercentage),
                AvgMonthlyContribution = Round(avgContrib),
                EstimatedMonthsToGoal = estMonths,
                EstimatedCompletionDate = estDate,
                IsOnTrack = isOnTrack
            });
        }

        return result;
    }

    // ── 6. Year Over Year ─────────────────────────────────────────────────

    public async Task<YearOverYearDto> GetYearOverYearAsync(
        Guid userId, int year, CancellationToken ct = default)
    {
        var months = new List<YoYMonthDto>();
        var monthNames = System.Globalization.CultureInfo.GetCultureInfo("es-EC").DateTimeFormat.AbbreviatedMonthNames;

        for (int m = 1; m <= 12; m++)
        {
            var currIncome = await _incomeRepo.GetTotalByUserAndPeriodAsync(userId, m, year, ct);
            var currExpenses = await _expenseRepo.GetTotalByUserAndPeriodAsync(userId, m, year, ct);
            var prevIncome = await _incomeRepo.GetTotalByUserAndPeriodAsync(userId, m, year - 1, ct);
            var prevExpenses = await _expenseRepo.GetTotalByUserAndPeriodAsync(userId, m, year - 1, ct);

            months.Add(new YoYMonthDto
            {
                MonthLabel = monthNames[m - 1],
                CurrentIncome = currIncome,
                CurrentExpenses = currExpenses,
                CurrentNetSavings = currIncome - currExpenses,
                PrevIncome = prevIncome,
                PrevExpenses = prevExpenses,
                PrevNetSavings = prevIncome - prevExpenses
            });
        }

        var totalCurrIncome = months.Sum(m => m.CurrentIncome);
        var totalPrevIncome = months.Sum(m => m.PrevIncome);
        var totalCurrExpenses = months.Sum(m => m.CurrentExpenses);
        var totalPrevExpenses = months.Sum(m => m.PrevExpenses);
        var totalCurrSavings = totalCurrIncome - totalCurrExpenses;
        var totalPrevSavings = totalPrevIncome - totalPrevExpenses;

        return new YearOverYearDto
        {
            Year = year,
            PreviousYear = year - 1,
            Months = months,
            Totals = new YoYTotalsDto
            {
                IncomeChangeAbs = Round(totalCurrIncome - totalPrevIncome),
                IncomeChangePct = Pct(totalCurrIncome - totalPrevIncome, totalPrevIncome),
                ExpensesChangeAbs = Round(totalCurrExpenses - totalPrevExpenses),
                ExpensesChangePct = Pct(totalCurrExpenses - totalPrevExpenses, totalPrevExpenses),
                NetSavingsChangeAbs = Round(totalCurrSavings - totalPrevSavings),
                NetSavingsChangePct = Pct(totalCurrSavings - totalPrevSavings, totalPrevSavings)
            }
        };
    }

    // ── 7. Budget vs Actual History ───────────────────────────────────────

    public async Task<BudgetVsActualHistoryDto> GetBudgetVsActualHistoryAsync(
        Guid userId, int months, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var periods = new List<BudgetVsActualPeriodDto>();
        var monthNames = System.Globalization.CultureInfo.GetCultureInfo("es-EC").DateTimeFormat.AbbreviatedMonthNames;

        for (int i = months - 1; i >= 0; i--)
        {
            var dt = today.AddMonths(-i);
            var budget = await _budgetRepo.GetByUserAndPeriodAsync(userId, dt.Month, dt.Year, ct);
            var actual = await _expenseRepo.GetTotalByUserAndPeriodAsync(userId, dt.Month, dt.Year, ct);
            var budgeted = budget?.TotalLimit ?? 0m;
            var variance = budgeted - actual;
            var adherence = budgeted > 0 ? Round(actual / budgeted * 100) : 0m;

            periods.Add(new BudgetVsActualPeriodDto
            {
                Label = $"{monthNames[dt.Month - 1]} {dt.Year % 100:D2}",
                Budgeted = budgeted,
                Actual = actual,
                Variance = Round(variance),
                AdherencePct = adherence
            });
        }

        return new BudgetVsActualHistoryDto { Periods = periods };
    }

    // ── Helpers privados ──────────────────────────────────────────────────

    private async Task EnsureCurrentSnapshotAsync(Guid userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var existing = await _snapshotRepo.GetByDateAsync(userId, monthStart, ct);
        if (existing is not null) return;

        var accounts = await _accountRepo.GetByUserIdAsync(userId, ct);
        var investments = await _investmentRepo.GetByUserIdAsync(userId, ct);
        var debts = await _debtRepo.GetByUserIdAsync(userId, ct);
        var cards = await _cardRepo.GetByUserIdAsync(userId, ct);

        var accountList = accounts.Where(a => !a.IsDeleted).ToList();
        var cashAccounts = Round(accountList.Where(a => a.Type == FinancialAccountType.Cash).Sum(a => a.CurrentBalance));
        var savingsAccounts = Round(accountList.Where(a => a.Type == FinancialAccountType.Savings).Sum(a => a.CurrentBalance));
        var investPositions = Round(investments.Where(i => !i.IsDeleted).Sum(i => i.CurrentValue));
        var debtLiab = Round(debts.Where(d => !d.IsDeleted && d.CurrentBalance > 0).Sum(d => d.CurrentBalance));
        var cardLiab = Round(cards.Where(c => !c.IsDeleted && c.CurrentBalance > 0).Sum(c => c.CurrentBalance));
        var totalAssets = Round(cashAccounts + savingsAccounts + investPositions);
        var totalLiab = Round(debtLiab + cardLiab);

        await _snapshotRepo.UpsertAsync(new NetWorthSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SnapshotDate = monthStart,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiab,
            NetWorth = Round(totalAssets - totalLiab),
            CashAccounts = cashAccounts,
            SavingsAccounts = savingsAccounts,
            InvestmentPositions = investPositions,
            DebtLiabilities = debtLiab,
            CreditCardLiabilities = cardLiab,
            Source = SnapshotSource.Automatic
        }, ct);
    }

    private async Task<decimal> GetEmergencyFundBalanceAsync(Guid userId, CancellationToken ct)
    {
        var goals = await _savingsRepo.GetByUserIdAsync(userId, ct);
        return goals
            .Where(g => g.Purpose == SavingsGoalPurpose.EmergencyFund && !g.IsDeleted)
            .Sum(g => g.CurrentAmount);
    }

    private async Task<decimal> GetAvgMonthlyExpensesAsync(Guid userId, int months, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = today;
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));
        var total = await _expenseRepo.GetTotalByDateRangeAsync(userId, start, end, ct);
        return months > 0 ? total / months : 0m;
    }

    private Task<decimal> GetInvestmentContributionsByPeriodAsync(
        Guid userId, DateOnly start, DateOnly end, CancellationToken ct) =>
        _investmentRepo.GetTotalContributionsByDateRangeAsync(userId, start, end, ct);

    // ── Score helpers ─────────────────────────────────────────────────────

    private static int ScoreFromRate(decimal value, decimal benchmark, bool ascending, decimal? cap = null)
    {
        if (ascending)
        {
            if (value <= 0) return 0;
            var effective = cap.HasValue ? Math.Min(value, cap.Value) : value;
            var capValue = cap ?? benchmark * 2;
            return (int)Math.Min(100, Math.Round(effective / capValue * 100));
        }
        else
        {
            if (value <= 0) return 100;
            if (value >= benchmark * 2) return 0;
            return (int)Math.Max(0, Math.Round((1 - value / (benchmark * 2)) * 100));
        }
    }

    private static string StatusFromScore(int score) => score switch
    {
        >= 70 => "Good",
        >= 40 => "Warning",
        _ => "Critical"
    };

    private static string GradeFromScore(int score) => score switch
    {
        >= 90 => "A",
        >= 75 => "B",
        >= 60 => "C",
        >= 45 => "D",
        _ => "F"
    };

    private static List<string> BuildRecommendations(
        int savingsScore, int dtiScore, int efScore, int expScore, int budgetScore, int investScore,
        decimal savingsRate, decimal dti, decimal efMonths, decimal expRatio, decimal budgetAdherence, decimal investRate)
    {
        var recs = new List<string>();

        if (savingsScore < 70)
            recs.Add($"Tu tasa de ahorro neto es {savingsRate:F1}%. Intenta alcanzar al menos el 20% de tus ingresos.");
        if (dtiScore < 70)
            recs.Add($"Tus pagos de deuda representan el {dti:F1}% de tus ingresos. Reducir a menos del 35% mejorará tu salud financiera.");
        if (efScore < 70)
            recs.Add($"Tu fondo de emergencia cubre {efMonths:F1} meses de gastos. La meta es al menos 3 meses.");
        if (expScore < 70)
            recs.Add($"Tus gastos son el {expRatio:F1}% de tus ingresos. Intenta mantenerte por debajo del 50%.");
        if (budgetScore < 70 && budgetAdherence > 0)
            recs.Add($"Ejecutaste el {budgetAdherence:F1}% de tu presupuesto mensual. Revisa las categorías con mayor desviación.");
        if (investScore < 70)
            recs.Add($"Estás invirtiendo el {investRate:F1}% de tus ingresos. Apunta al 10% para crecer tu patrimonio.");

        if (recs.Count == 0)
            recs.Add("¡Excelente salud financiera! Mantén el ritmo de ahorro e inversión.");

        return recs;
    }

    private static decimal AnnualImpact(decimal amount, RecurrenceType? recurrence) => recurrence switch
    {
        RecurrenceType.Daily => amount * 365,
        RecurrenceType.Weekly => amount * 52,
        RecurrenceType.Biweekly => amount * 26,
        RecurrenceType.Yearly => amount,
        _ => amount * 12
    };

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    private static decimal Pct(decimal change, decimal base_) =>
        base_ != 0 ? Math.Round(change / base_ * 100, 2) : 0m;
}
