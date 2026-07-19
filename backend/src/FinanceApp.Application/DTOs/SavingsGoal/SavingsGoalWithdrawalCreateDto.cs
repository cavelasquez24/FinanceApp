using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.DTOs.SavingsGoal;

public class SavingsGoalWithdrawalCreateDto
{
    public decimal Amount { get; set; }               // > 0
    public DateOnly? WithdrawalDate { get; set; }      // null → hoy
    public SavingsWithdrawalReason Reason { get; set; }
    public Guid? LinkedExpenseId { get; set; }         // solo si Reason = Consumed
    public string? Notes { get; set; }
}
