namespace FinanceApp.Application.DTOs.Analytics;

public class FinancialHealthScoreDto
{
    public int Score { get; set; }
    public string Grade { get; set; } = string.Empty;
    public HealthScoreComponents Components { get; set; } = new();
    public List<string> Recommendations { get; set; } = [];
}

public class HealthScoreComponents
{
    public ScoreComponent SavingsRate { get; set; } = new();
    public ScoreComponent DebtToIncome { get; set; } = new();
    public ScoreComponent EmergencyFundCoverage { get; set; } = new();
    public ScoreComponent ExpenseRatio { get; set; } = new();
    public ScoreComponent BudgetAdherence { get; set; } = new();
    public ScoreComponent InvestmentRate { get; set; } = new();
}

public class ScoreComponent
{
    public int Score { get; set; }
    public decimal Value { get; set; }
    public decimal Benchmark { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
