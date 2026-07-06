namespace FinanceApp.Application.DTOs.Category;

public class CategoryCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "expense";
    public string? Icon { get; set; }
    public string Color { get; set; } = "#6B7280";
}