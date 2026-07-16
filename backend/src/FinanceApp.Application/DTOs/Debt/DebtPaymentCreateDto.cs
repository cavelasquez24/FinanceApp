namespace FinanceApp.Application.DTOs.Debt;

public class DebtPaymentCreateDto
{
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }   // > 0, reduce el saldo
    public decimal InterestAmount { get; set; } = 0;
    public string? Notes { get; set; }
}