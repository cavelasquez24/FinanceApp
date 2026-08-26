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
    Correction,

    /// <summary>
    /// Préstamo temporal a sí mismo — el dinero sale hacia una cuenta
    /// líquida (DestinationAccountId obligatorio) con el compromiso de
    /// reponerlo. Neutro en patrimonio.
    ///
    /// Solo válido cuando SavingsGoal.Purpose == EmergencyFund.
    /// La validación se aplica en SavingsGoalService.WithdrawAsync.
    ///
    /// El compromiso de devolución se modela exclusivamente con
    /// EmergencyFundRestoration; las metas generales nunca prestan.
    /// </summary>
    TemporaryLoan
}
