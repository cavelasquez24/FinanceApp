using FinanceApp.Application.DTOs.Investment;
using FluentValidation;

namespace FinanceApp.Application.Validators;

public class InvestmentCreateValidator : AbstractValidator<InvestmentCreateDto>
{
    private static readonly string[] ValidTypes =
        { "etf", "stock", "mutualfund", "crypto", "bond", "other" };

    public InvestmentCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("El tipo es requerido")
            .Must(t => ValidTypes.Contains(t.ToLower()))
            .WithMessage($"Tipo inválido. Valores permitidos: {string.Join(", ", ValidTypes)}");

        RuleFor(x => x.InitialAmount)
            .GreaterThan(0).WithMessage("El monto inicial debe ser mayor a 0");

        RuleFor(x => x.CurrentValue)
            .GreaterThanOrEqualTo(0).WithMessage("El valor actual no puede ser negativo");

        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("La fecha de compra es requerida")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("La fecha de compra no puede ser futura");

        RuleFor(x => x.Ticker)
            .MaximumLength(20).WithMessage("El ticker no puede superar 20 caracteres")
            .When(x => x.Ticker != null);
    }
}