namespace FinanceApp.Domain.Enums;

public enum InvestmentTransactionType
{
    Contribution,           // Aporte de capital nuevo
    HistoricalContribution, // Aporte pasado, no toca el ciclo presupuestario actual
    Withdrawal,             // Retiro/rescate
    Valuation,              // Actualización de valor de mercado
    Dividend,               // Dividendo distribuido a efectivo
    DividendReinvested,     // Dividendo reinvertido, no genera caja
    Fee,                    // Comisión asociada a una operación
    Reversal                // Anulación de otra transacción
}
