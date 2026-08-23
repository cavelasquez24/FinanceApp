using System.Text.Json.Serialization;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.DTOs.SavingsGoal;

public class SavingsReplenishmentCreateDto
{
    public Guid SavingsGoalId { get; set; }
    public Guid SourceAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal AmountTaken { get; set; }
    public decimal MonthlyDebitAmount { get; set; }
    public bool AutoDebitEnabled { get; set; } = true;
}

public class SavingsReplenishmentManualDebitDto
{
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public Guid IdempotencyKey { get; set; }
}

public class SavingsReplenishmentPauseDto
{
    public string? Reason { get; set; }
}

public class SavingsReplenishmentDto
{
    public Guid Id { get; set; }
    public Guid SavingsGoalId { get; set; }
    public string SavingsGoalName { get; set; } = string.Empty;
    public Guid SourceAccountId { get; set; }
    public string SourceAccountName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public decimal AmountTaken { get; set; }
    public decimal AmountReplenished { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal MonthlyDebitAmount { get; set; }
    public decimal ProgressPercent { get; set; }
    public int EstimatedCyclesRemaining { get; set; }

    public bool AutoDebitEnabled { get; set; }
    public bool IsPaused { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReplenishmentStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public DateOnly? LastDebitAt { get; set; }
    public List<ReplenishmentDebitDto> Debits { get; set; } = new();
}

/// <summary>
/// Un débito (automático o manual) de una SavingsReplenishment. Se lee
/// directamente de SavingsGoalContribution filtrando por
/// SavingsReplenishmentId — no existe una tabla de historial separada.
/// </summary>
public class ReplenishmentDebitDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DebitDate { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DebitType Type { get; set; }
    public string? Notes { get; set; }
}

public class ReplenishmentCycleResultDto
{
    public int ProcessedCount { get; set; }
    public int SkippedAlreadyDebitedCount { get; set; }
    public List<ReplenishmentDebitFailureDto> InsufficientFunds { get; set; } = new();
}

public class ReplenishmentDebitFailureDto
{
    public Guid ReplenishmentId { get; set; }
    public string ReplenishmentName { get; set; } = string.Empty;
    public decimal RequiredAmount { get; set; }
    public decimal AvailableBalance { get; set; }
}
