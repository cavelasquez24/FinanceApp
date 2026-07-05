namespace FinanceApp.Domain.Entities;

/// <summary>
/// Centraliza los campos de auditoría para no repetirlos en cada entidad.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Identificador único universal (GUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Fecha y hora de creación del registro.
    /// DateTimeOffset incluye información de zona horaria.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Fecha y hora de la última modificación.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Soft Delete: NULL = activo, fecha = eliminado.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Propiedad calculada.
    /// </summary>
    public bool IsDeleted => DeletedAt.HasValue;
}