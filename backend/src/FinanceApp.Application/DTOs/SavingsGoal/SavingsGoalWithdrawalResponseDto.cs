using FinanceApp.Domain.Enums;
using System.Text.Json.Serialization;

namespace FinanceApp.Application.DTOs.SavingsGoal;

public class SavingsGoalWithdrawalResponseDto
{
    public Guid Id { get; set; }
    public DateOnly WithdrawalDate { get; set; }
    public decimal Amount { get; set; }
    public Guid? LinkedExpenseId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SavingsWithdrawalReason Reason { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Estado resultante de la meta tras el retiro, útil para actualizar UI sin refetch
    public decimal GoalCurrentAmountAfter { get; set; }
}
