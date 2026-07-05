namespace FinanceApp.Domain.Entities;

public class RefreshToken
{
    /// <summary>
    /// No hereda de BaseEntity porque los tokens
    /// se crean y revocan, nunca se modifican.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    /// <summary>
    /// Token hasheado con SHA-256.
    /// Nunca almacenamos el token en texto plano.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? CreatedByIp { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Propiedad de navegación
    public User User { get; set; } = null!;

    // Calculadas en memoria
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}