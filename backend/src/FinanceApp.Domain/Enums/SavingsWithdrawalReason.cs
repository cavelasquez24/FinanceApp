namespace FinanceApp.Domain.Enums;

/// <summary>
/// Motivo del retiro de una SavingsGoal.
/// Determina si el flujo de UI debe ofrecer vincular un Expense (solo
/// cuando Consumed) o si el movimiento es neutro en patrimonio.
/// </summary>
public enum SavingsWithdrawalReason
{
    Consumed,
    ReallocatedToOtherGoal,
    ReallocatedToLiquid,
    Correction
}
