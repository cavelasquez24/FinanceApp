using FinanceApp.Application.DTOs.Income;
using FluentValidation;

namespace FinanceApp.Application.Validators;

public class IncomeCreateValidator : AbstractValidator<IncomeCreateDto>
{
    public IncomeCreateValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("La categoría es requerida");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0")
            .LessThanOrEqualTo(9999999999999.99m)
            .WithMessage("El monto excede el máximo permitido");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha es requerida")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("La fecha no puede ser futura");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("La descripción no puede superar 500 caracteres")
            .When(x => x.Description != null);

        RuleFor(x => x.Source)
            .MaximumLength(200)
            .WithMessage("La fuente no puede superar 200 caracteres")
            .When(x => x.Source != null);
    }
}