namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentRecordCreateDto
{
    public DateOnly RecordDate { get; set; }
    public decimal Value { get; set; }
    public decimal Dividends { get; set; } = 0;
    public string? Notes { get; set; }
}