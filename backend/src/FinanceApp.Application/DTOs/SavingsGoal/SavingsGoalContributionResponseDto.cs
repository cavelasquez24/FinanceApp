namespace FinanceApp.Application.DTOs.SavingsGoal;

/// <summary>
/// Respuesta de un item de historial de contribución.
/// </summary>
public class SavingsGoalContributionResponseDto
{
    public Guid Id { get; set; }
    public DateOnly ContributionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
