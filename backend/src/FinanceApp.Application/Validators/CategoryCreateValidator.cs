using FinanceApp.Application.DTOs.Category;
using FluentValidation;

namespace FinanceApp.Application.Validators;

public class CategoryCreateValidator : AbstractValidator<CategoryCreateDto>
{
    private static readonly string[] ValidTypes = { "income", "expense", "both" };

    public CategoryCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");

        RuleFor(x => x.Type)
            .Must(t => ValidTypes.Contains(t.ToLower()))
            .WithMessage("Tipo inválido. Valores permitidos: income, expense, both");

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("El color debe estar en formato HEX. Ejemplo: #FF5733")
            .When(x => x.Color != null);
    }
}