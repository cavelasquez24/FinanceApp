using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Plan de reposición programada hacia una SavingsGoal propia — el usuario
/// tomó dinero prestado de su propia meta (no es un pasivo externo) y se
/// compromete a reponerlo con débitos automáticos por ciclo desde una
/// cuenta operativa. Neutro en patrimonio: solo redistribuye entre la
/// cuenta origen y la meta.
/// </summary>
public class SavingsReplenishment : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SavingsGoalId { get; set; }
    public Guid SourceAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public decimal AmountTaken { get; set; }
    public decimal AmountReplenished { get; set; }
    public decimal MonthlyDebitAmount { get; set; }
    public bool AutoDebitEnabled { get; set; } = true;
    public bool IsPaused { get; set; }

    public ReplenishmentStatus Status { get; set; } = ReplenishmentStatus.Active;
    public DateOnly? CompletedAt { get; set; }
    public DateOnly? LastDebitAt { get; set; }

    public SavingsGoal SavingsGoal { get; set; } = null!;
    public FinancialAccount SourceAccount { get; set; } = null!;

    public decimal PendingAmount => Math.Max(AmountTaken - AmountReplenished, 0);
}
