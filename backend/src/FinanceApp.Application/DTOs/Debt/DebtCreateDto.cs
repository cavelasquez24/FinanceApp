namespace FinanceApp.Application.DTOs.Debt;

public class DebtCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;      // snake_case: "credit_card"
    public string? Creditor { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? MinimumPayment { get; set; }
    public int? DueDay { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? TargetPayoffDate { get; set; }
    public string? Notes { get; set; }
}