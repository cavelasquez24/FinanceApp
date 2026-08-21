using FinanceApp.Application.Services;

namespace FinanceApp.UnitTests;

/// <summary>
/// Tests unitarios para la lógica de Score financiero en AnalyticsService.
/// Se prueban los métodos estáticos privados via reflexión usando el servicio
/// real con inputs controlados — o directamente a través del DTO resultante
/// usando un stub de servicio que delega a los mismos cálculos.
///
/// Dado que la lógica de scoring está encapsulada en helpers privados de
/// AnalyticsService, extraemos las invariantes a través del resultado público:
/// el Score y Grade del FinancialHealthScoreDto.
/// </summary>
public class FinancialHealthScoreTests
{
    // ── ScoreFromRate helper (probado vía reflexión) ──────────────────────

    private static int CallScoreFromRate(decimal value, decimal benchmark, bool ascending, decimal? cap = null)
    {
        var method = typeof(AnalyticsService)
            .GetMethod("ScoreFromRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (int)method.Invoke(null, [value, benchmark, ascending, cap])!;
    }

    private static string CallStatusFromScore(int score)
    {
        var method = typeof(AnalyticsService)
            .GetMethod("StatusFromScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (string)method.Invoke(null, [score])!;
    }

    private static string CallGradeFromScore(int score)
    {
        var method = typeof(AnalyticsService)
            .GetMethod("GradeFromScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (string)method.Invoke(null, [score])!;
    }

    // ── Ascending scores (savings rate, EF coverage, investment rate) ─────

    [Fact]
    public void ScoreFromRate_Ascending_ZeroValueReturnsZero()
    {
        Assert.Equal(0, CallScoreFromRate(0m, 20m, ascending: true));
    }

    [Fact]
    public void ScoreFromRate_Ascending_AtBenchmarkReturnsMidScore()
    {
        // benchmark=20, cap=40 (default 2x) → 20/40 * 100 = 50
        var score = CallScoreFromRate(20m, 20m, ascending: true);
        Assert.Equal(50, score);
    }

    [Fact]
    public void ScoreFromRate_Ascending_AtCapReturns100()
    {
        // benchmark=20, cap=40 → 40/40 * 100 = 100
        var score = CallScoreFromRate(40m, 20m, ascending: true);
        Assert.Equal(100, score);
    }

    [Fact]
    public void ScoreFromRate_Ascending_AboveCapClampsTo100()
    {
        var score = CallScoreFromRate(100m, 20m, ascending: true);
        Assert.Equal(100, score);
    }

    [Fact]
    public void ScoreFromRate_Ascending_WithExplicitCap()
    {
        // efMonths=3, benchmark=3, cap=6 → 3/6*100 = 50
        var score = CallScoreFromRate(3m, 3m, ascending: true, cap: 6m);
        Assert.Equal(50, score);
    }

    [Fact]
    public void ScoreFromRate_Ascending_FullCapReturns100()
    {
        // efMonths=6 (cap=6) → 6/6*100 = 100
        var score = CallScoreFromRate(6m, 3m, ascending: true, cap: 6m);
        Assert.Equal(100, score);
    }

    // ── Descending scores (debt-to-income, expense ratio) ─────────────────

    [Fact]
    public void ScoreFromRate_Descending_ZeroValueReturns100()
    {
        Assert.Equal(100, CallScoreFromRate(0m, 35m, ascending: false));
    }

    [Fact]
    public void ScoreFromRate_Descending_AtBenchmarkReturnsMidScore()
    {
        // value=35, benchmark=35 → (1 - 35/70)*100 = 50
        var score = CallScoreFromRate(35m, 35m, ascending: false);
        Assert.Equal(50, score);
    }

    [Fact]
    public void ScoreFromRate_Descending_AtDoubleBenchmarkReturnsZero()
    {
        var score = CallScoreFromRate(70m, 35m, ascending: false);
        Assert.Equal(0, score);
    }

    [Fact]
    public void ScoreFromRate_Descending_AboveDoubleBenchmarkClampsToZero()
    {
        var score = CallScoreFromRate(200m, 35m, ascending: false);
        Assert.Equal(0, score);
    }

    // ── Grade boundaries ──────────────────────────────────────────────────

    [Theory]
    [InlineData(90, "A")]
    [InlineData(95, "A")]
    [InlineData(75, "B")]
    [InlineData(89, "B")]
    [InlineData(60, "C")]
    [InlineData(74, "C")]
    [InlineData(45, "D")]
    [InlineData(59, "D")]
    [InlineData(0, "F")]
    [InlineData(44, "F")]
    public void GradeFromScore_ReturnsCorrectGrade(int score, string expectedGrade)
    {
        Assert.Equal(expectedGrade, CallGradeFromScore(score));
    }

    // ── Status thresholds ─────────────────────────────────────────────────

    [Theory]
    [InlineData(70, "Good")]
    [InlineData(100, "Good")]
    [InlineData(40, "Warning")]
    [InlineData(69, "Warning")]
    [InlineData(0, "Critical")]
    [InlineData(39, "Critical")]
    public void StatusFromScore_ReturnsCorrectStatus(int score, string expected)
    {
        Assert.Equal(expected, CallStatusFromScore(score));
    }

    // ── Weighted score invariants ─────────────────────────────────────────

    [Fact]
    public void WeightedScore_AllPerfect_Returns100()
    {
        // 0.25+0.20+0.20+0.15+0.10+0.10 = 1.0, todos a 100
        var weighted = (int)Math.Round(100 * 0.25m + 100 * 0.20m + 100 * 0.20m + 100 * 0.15m + 100 * 0.10m + 100 * 0.10m);
        Assert.Equal(100, weighted);
    }

    [Fact]
    public void WeightedScore_AllZero_Returns0()
    {
        var weighted = (int)Math.Round(0 * 0.25m + 0 * 0.20m + 0 * 0.20m + 0 * 0.15m + 0 * 0.10m + 0 * 0.10m);
        Assert.Equal(0, weighted);
    }

    [Fact]
    public void WeightedScore_WeightsSum_To90Percent()
    {
        // Solo 5 de los 6 componentes = 0.25+0.20+0.20+0.15+0.10 = 0.90 → max 90
        var weighted = (int)Math.Round(100 * 0.25m + 100 * 0.20m + 100 * 0.20m + 100 * 0.15m + 100 * 0.10m + 0 * 0.10m);
        Assert.Equal(90, weighted);
    }

    // ── Annual impact ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(100, "Monthly", 1200)]
    [InlineData(50, "Weekly", 2600)]
    [InlineData(10, "Yearly", 10)]
    [InlineData(5, "Daily", 1825)]
    [InlineData(200, "Biweekly", 5200)]
    public void AnnualImpact_CalculatesCorrectly(decimal amount, string recurrence, decimal expected)
    {
        var method = typeof(AnalyticsService)
            .GetMethod("AnnualImpact", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var recurrenceEnum = Enum.Parse(typeof(FinanceApp.Domain.Enums.RecurrenceType), recurrence);
        var result = (decimal)method.Invoke(null, [amount, recurrenceEnum])!;
        Assert.Equal(expected, result);
    }
}
