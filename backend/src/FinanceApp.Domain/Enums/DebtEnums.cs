namespace FinanceApp.Domain.Enums;

/// <summary>
/// Tipos de deuda/pasivo financiero.
/// </summary>
public enum DebtType
{
    CreditCard,     // Tarjeta de crédito
    Loan,           // Préstamo personal
    Mortgage,       // Hipoteca
    Personal,       // Deuda con persona (no institucional)
    Other           // Otro tipo
}