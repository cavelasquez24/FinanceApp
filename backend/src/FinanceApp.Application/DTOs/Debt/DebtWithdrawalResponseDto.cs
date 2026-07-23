namespace FinanceApp.Application.DTOs.Debt;

public class DebtWithdrawalResponseDto
{
    public Guid Id { get; set; }
    public DateOnly WithdrawalDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public decimal DebtCurrentBalanceAfter { get; set; }
}