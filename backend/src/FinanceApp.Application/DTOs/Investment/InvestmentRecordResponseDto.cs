namespace FinanceApp.Application.DTOs.Investment;

public class InvestmentRecordResponseDto
{
    public Guid Id { get; set; }
    public DateOnly RecordDate { get; set; }
    public decimal Value { get; set; }
    public decimal Dividends { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}