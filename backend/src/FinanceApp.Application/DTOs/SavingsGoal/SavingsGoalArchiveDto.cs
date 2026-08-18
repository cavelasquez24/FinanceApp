namespace FinanceApp.Application.DTOs.SavingsGoal;

/// <summary>Decisión obligatoria al archivar una meta con saldo.</summary>
public class SavingsGoalArchiveDto
{
    /// <summary>release | reassign</summary>
    public string Resolution { get; set; } = string.Empty;
    public Guid? DestinationAccountId { get; set; }
    public Guid? TargetGoalId { get; set; }
    public DateOnly? Date { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string? Notes { get; set; }
}
