namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentWithdrawalDto
{
    public decimal WithdrawalAmount { get; set; }
    public decimal CapitalReturned { get; set; }
    public decimal Fee { get; set; } = 0m;
    public DateOnly WithdrawalDate { get; set; }
    public string? Notes { get; set; }
}

public class InvestmentWithdrawalResponseDto
{
    public Guid InvestmentId { get; set; }
    public decimal WithdrawalAmount { get; set; }
    public decimal CapitalReturned { get; set; }
    public decimal RealizedGain { get; set; }
    public decimal Fee { get; set; }
    public decimal NetCashReceived { get; set; }
    public DateOnly WithdrawalDate { get; set; }
    public decimal RemainingContributedCapital { get; set; }
    public decimal RemainingCurrentValue { get; set; }
}
