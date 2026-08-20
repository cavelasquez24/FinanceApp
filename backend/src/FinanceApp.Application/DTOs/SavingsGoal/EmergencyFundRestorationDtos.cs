namespace FinanceApp.Application.DTOs.SavingsGoal;

public class EmergencyFundUseCreateDto
{
    public decimal FundedAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UseMode { get; set; } = "expense";
    public Guid? DestinationAccountId { get; set; }
    public Guid? ExpenseCategoryId { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public DateOnly TargetRestorationDate { get; set; }
    public decimal ScheduledContributionAmount { get; set; }
    public DateOnly FirstScheduledDate { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string? Notes { get; set; }
}

public class EmergencyFundRestorationPaymentDto
{
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string FundingMode { get; set; } = "account_transfer";
    public Guid? SourceAccountId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string? Notes { get; set; }
}

public class EmergencyFundRestorationResponseDto
{
    public Guid Id { get; set; }
    public Guid SavingsGoalId { get; set; }
    public Guid? LinkedExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RestoredAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateOnly TargetRestorationDate { get; set; }
    public decimal ScheduledContributionAmount { get; set; }
    public decimal NextContributionAmount { get; set; }
    public DateOnly NextScheduledDate { get; set; }
    public DateOnly? EstimatedCompletionDate { get; set; }
    public Guid? ScheduledSourceAccountId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? CompletedDate { get; set; }
    public bool IsOverdue { get; set; }
    public string? Notes { get; set; }
}

