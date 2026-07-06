namespace FinanceApp.Application.DTOs.Category;

public class CategoryUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Color { get; set; } = "#6B7280";
}