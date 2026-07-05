namespace FinanceApp.Domain.Enums;

/// <summary>
/// Tipos de categoría según el flujo de dinero que clasifican.
/// </summary>
public enum CategoryType
{
    Income,     // Solo para ingresos
    Expense,    // Solo para gastos
    Both        // Aplica para ambos
}

/// <summary>
/// Métodos de pago para registrar gastos.
/// </summary>
public enum PaymentMethod
{
    Cash,           // Efectivo
    DebitCard,      // Tarjeta de débito
    CreditCard,     // Tarjeta de crédito
    Transfer,       // Transferencia bancaria
    Other           // Otro método
}

/// <summary>
/// Frecuencia de repetición de un gasto recurrente.
/// </summary>
public enum RecurrenceType
{
    Daily,      // Diario
    Weekly,     // Semanal
    Biweekly,   // Quincenal
    Monthly,    // Mensual
    Yearly      // Anual
}

/// <summary>
/// Tipos de activos de inversión.
/// </summary>
public enum InvestmentType
{
    ETF,
    Stock,          // Acciones
    MutualFund,     // Fondo mutuo
    Crypto,         // Criptomonedas
    Bond,           // Bonos
    Other
}