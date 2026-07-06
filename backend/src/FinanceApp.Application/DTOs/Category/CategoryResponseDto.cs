namespace FinanceApp.Application.DTOs.Category;

public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}