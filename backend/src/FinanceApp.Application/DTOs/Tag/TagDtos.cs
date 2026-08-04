namespace FinanceApp.Application.DTOs.Tag;

public sealed class TagResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int UsageCount { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}

public sealed class TagCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public sealed class TagUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public sealed class TagMergeDto
{
    public Guid TargetTagId { get; set; }
}

public sealed class TagExpenseReportDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalExpenses { get; set; }
    public int TaggedExpenses { get; set; }
    public int UntaggedExpenses => TotalExpenses - TaggedExpenses;
    public decimal CoveragePercentage => TotalExpenses == 0 ? 0 : Math.Round(TaggedExpenses * 100m / TotalExpenses, 2);
    public List<TagExpenseReportItemDto> Tags { get; set; } = new();
}

public sealed class TagExpenseReportItemDto
{
    public Guid TagId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public decimal TotalAmount { get; set; }
    public int ExpenseCount { get; set; }
    public decimal AverageAmount { get; set; }
}
