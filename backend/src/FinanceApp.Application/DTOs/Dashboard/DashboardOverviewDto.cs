namespace FinanceApp.Application.DTOs.Dashboard;

/// <summary>
/// Resumen principal del Dashboard.
/// Contiene todos los indicadores financieros del mes.
/// </summary>
public class DashboardOverviewDto
{
    public PeriodDto Period { get; set; } = null!;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetSavings { get; set; }
    public decimal SavingsRate { get; set; }
    public decimal TotalDebt { get; set; }           // saldo pendiente total, no depende de rango
    public decimal TotalDebtPayments { get; set; }    // pagado en este ciclo
    public decimal TotalInvestments { get; set; }
    public decimal TotalSavingsGoals { get; set; }
    public decimal NetWorth { get; set; }
    public PreviousMonthDto PreviousMonth { get; set; } = null!;
    public ChangesDto Changes { get; set; } = null!;
}

public class PeriodDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class PreviousMonthDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetSavings { get; set; }
    public decimal TotalDebtPayments { get; set; }

}

public class ChangesDto
{
    /// <summary>
    /// Porcentaje de cambio vs mes anterior.
    /// Positivo = mejoró, Negativo = empeoró.
    /// </summary>
    public decimal IncomeChange { get; set; }
    public decimal ExpensesChange { get; set; }
    public decimal SavingsChange { get; set; }
    public decimal DebtPaymentsChange { get; set; }

}