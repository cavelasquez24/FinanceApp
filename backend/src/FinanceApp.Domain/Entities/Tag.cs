namespace FinanceApp.Domain.Entities;

public class Tag : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public Guid? MergedIntoTagId { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    public User User { get; set; } = null!;
    public Tag? MergedIntoTag { get; set; }
    public ICollection<ExpenseTag> ExpenseTags { get; set; } = new List<ExpenseTag>();
}
