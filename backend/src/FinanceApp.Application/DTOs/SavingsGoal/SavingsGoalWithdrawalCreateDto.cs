using FinanceApp.Domain.Enums;
using System.Text.Json.Serialization;

namespace FinanceApp.Application.DTOs.SavingsGoal;

public class SavingsGoalWithdrawalCreateDto
{
    public decimal Amount { get; set; }               // > 0
    public DateOnly? WithdrawalDate { get; set; }      // null → hoy
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SavingsWithdrawalReason Reason { get; set; }
    public Guid? DestinationAccountId { get; set; }
    public Guid? TargetGoalId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public Guid? LinkedExpenseId { get; set; }         // solo si Reason = Consumed
    public string? Notes { get; set; }
}
