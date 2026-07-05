using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Category : BaseEntity
{
    /// <summary>
    /// NULL para categorías del sistema, UUID para categorías del usuario.
    /// </summary>
    public Guid? UserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; } = CategoryType.Expense;

    /// <summary>
    /// Nombre del icono de la librería del frontend.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Color HEX para gráficos.
    /// </summary>
    public string Color { get; set; } = "#6B7280";

    /// <summary>
    /// Las categorías del sistema no pueden eliminarse.
    /// </summary>
    public bool IsDefault { get; set; } = false;

    // Propiedades de navegación
    public User? User { get; set; }
    public ICollection<Income> Incomes { get; set; } = new List<Income>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}