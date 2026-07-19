namespace FinanceApp.Domain.Entities;

/// <summary>
/// Registro histórico de un aporte a una SavingsGoal.
/// Efecto: incrementa SavingsGoal.CurrentAmount. NUNCA crea un Expense
/// (v2.0.1 — el aporte es una transferencia, no un consumo).
/// </summary>
public class SavingsGoalContribution : BaseEntity
{
    public Guid SavingsGoalId { get; set; }
    public DateOnly ContributionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }

    // Propiedad de navegación
    public SavingsGoal SavingsGoal { get; set; } = null!;
}
