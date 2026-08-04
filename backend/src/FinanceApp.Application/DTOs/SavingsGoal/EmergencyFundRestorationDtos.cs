namespace FinanceApp.Application.DTOs.SavingsGoal;

public class EmergencyFundUseCreateDto
{
    public decimal FundedAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? ExpenseAccountId { get; set; }
    public Guid? ScheduledSourceAccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public DateOnly TargetRestorationDate { get; set; }
    public decimal ScheduledContributionAmount { get; set; }
    public DateOnly FirstScheduledDate { get; set; }
    public string? Notes { get; set; }
}

public class EmergencyFundRestorationPaymentDto
{
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public Guid? SourceAccountId { get; set; }
    public string? Notes { get; set; }
}

public class EmergencyFundRestorationResponseDto
{
    public Guid Id { get; set; }
    public Guid SavingsGoalId { get; set; }
    public Guid LinkedExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RestoredAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateOnly TargetRestorationDate { get; set; }
    public decimal ScheduledContributionAmount { get; set; }
    public decimal NextContributionAmount { get; set; }
    public Guid? ScheduledSourceAccountId { get; set; }
    public DateOnly NextScheduledDate { get; set; }
    public DateOnly? EstimatedCompletionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? CompletedDate { get; set; }
    public bool IsOverdue { get; set; }
    public string? Notes { get; set; }
}

public class DueRestorationProcessingResultDto
{
    public int ProcessedCount { get; set; }
    public decimal ProcessedAmount { get; set; }
    public int InsufficientFundsCount { get; set; }
}
