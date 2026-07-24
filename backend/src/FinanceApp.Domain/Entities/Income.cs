namespace FinanceApp.Domain.Entities;

public class Income : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? AccountId { get; set; }

    /// <summary>
    /// Precisión exacta especifica para dinero.
    /// </summary>
    public decimal Amount { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Solo fecha sin hora.
    /// </summary>
    public DateOnly Date { get; set; }
    /// <summary>
    /// Permite que un ingreso recibido pocos d?as antes del pago financie
    /// el siguiente ciclo sin modificar su fecha bancaria real.
    /// </summary>
    public DateOnly? AssignedCycleStart { get; set; }


    public string? Source { get; set; }

    // Propiedades de navegación
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public FinancialAccount? Account { get; set; }
}