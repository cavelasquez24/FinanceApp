namespace FinanceApp.Application.DTOs.Debt;

public class DebtUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Creditor { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? MinimumPayment { get; set; }
    public int? DueDay { get; set; }
    public DateOnly? TargetPayoffDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}