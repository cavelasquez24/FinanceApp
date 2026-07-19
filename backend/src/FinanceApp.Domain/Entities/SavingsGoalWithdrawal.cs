using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Registro histórico de un retiro de una SavingsGoal.
/// Efecto: reduce SavingsGoal.CurrentAmount (Math.Max(0, ...), igual
/// patrón que Debt.CurrentBalance).
///
/// El retiro por sí solo es neutro en patrimonio (mueve stock de "meta"
/// a "líquido"). Solo si Reason = Consumed y hay un Expense vinculado
/// (LinkedExpenseId) se reduce efectivamente el patrimonio neto — ese
/// Expense es responsabilidad del flujo de UI/otro módulo, no de esta
/// entidad.
/// </summary>
public class SavingsGoalWithdrawal : BaseEntity
{
    public Guid SavingsGoalId { get; set; }
    public DateOnly WithdrawalDate { get; set; }
    public decimal Amount { get; set; }

    /// <summary>
    /// FK opcional a Expense. Solo debe poblarse cuando Reason = Consumed.
    /// Sin navegación obligatoria: Expense pertenece a otro agregado.
    /// </summary>
    public Guid? LinkedExpenseId { get; set; }

    public SavingsWithdrawalReason Reason { get; set; }
    public string? Notes { get; set; }

    // Propiedad de navegación
    public SavingsGoal SavingsGoal { get; set; } = null!;
}
