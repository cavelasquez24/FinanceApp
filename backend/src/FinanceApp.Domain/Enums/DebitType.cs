namespace FinanceApp.Domain.Enums;

/// <summary>
/// Cómo se originó un aporte que repone una SavingsReplenishment —
/// usado en SavingsGoalContribution.DebitType.
/// </summary>
public enum DebitType
{
    Automatic = 1,
    Manual = 2,
    Adjustment = 3
}
