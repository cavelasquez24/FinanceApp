using FinanceApp.Application.DTOs.Budget;
using FluentValidation;

namespace FinanceApp.Application.Validators;

public class BudgetCreateValidator : AbstractValidator<BudgetCreateDto>
{
    public BudgetCreateValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("El mes debe estar entre 1 y 12");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("El año debe estar entre 2000 y 2100");

        RuleFor(x => x.TotalLimit)
            .GreaterThan(0)
            .WithMessage("El límite total debe ser mayor a 0")
            .When(x => x.TotalLimit.HasValue);

        RuleFor(x => x.Categories)
            .NotEmpty()
            .WithMessage("Debe incluir al menos una categoría en el presupuesto");

        RuleForEach(x => x.Categories).ChildRules(category =>
        {
            category.RuleFor(c => c.CategoryId)
                .NotEmpty().WithMessage("La categoría es requerida");

            category.RuleFor(c => c.AmountLimit)
                .GreaterThan(0).WithMessage("El límite por categoría debe ser mayor a 0");
        });
    }
}