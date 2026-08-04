using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Models;

public sealed class ExpenseQueryOptions
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? CategoryId { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string? Search { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public bool? IsRecurring { get; init; }
    public string SortBy { get; init; } = "date";
    public bool SortDescending { get; init; } = true;
    public IReadOnlyCollection<Guid> TagIds { get; init; } = Array.Empty<Guid>();
    public bool MatchAllTags { get; init; }
    public bool Untagged { get; init; }
    public string? Merchant { get; init; }
}
