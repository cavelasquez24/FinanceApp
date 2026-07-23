namespace FinanceApp.Application.DTOs.Debt;

public class DebtResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Creditor { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal PaidPercentage { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? MinimumPayment { get; set; }
    public int? DueDay { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? TargetPayoffDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsPaidOff { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? LinkedSavingsGoalId { get; set; }
}