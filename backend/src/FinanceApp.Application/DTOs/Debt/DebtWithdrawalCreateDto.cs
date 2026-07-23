namespace FinanceApp.Application.DTOs.Debt;

public class DebtWithdrawalCreateDto
{
    public decimal Amount { get; set; }             // > 0
    public DateOnly? WithdrawalDate { get; set; }    // null → hoy
    public string? Notes { get; set; }
}