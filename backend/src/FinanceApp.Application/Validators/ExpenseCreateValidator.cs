using FinanceApp.Application.DTOs.Expense;
using FluentValidation;

namespace FinanceApp.Application.Validators;

public class ExpenseCreateValidator : AbstractValidator<ExpenseCreateDto>
{
    private static readonly string[] ValidPaymentMethods =
        { "cash", "debit_card", "credit_card", "transfer", "other" };

    private static readonly string[] ValidRecurrenceTypes =
        { "daily", "weekly", "biweekly", "monthly", "yearly" };

    public ExpenseCreateValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("La categoría es requerida");

        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Selecciona explícitamente la cuenta desde la que se pagó el gasto")
            .When(x => x.PaymentMethod != "credit_card");

        RuleFor(x => x.AccountId)
            .Empty().WithMessage("Una compra con tarjeta de crédito no debe descontar una cuenta de caja")
            .When(x => x.PaymentMethod == "credit_card");

        RuleFor(x => x.CreditCardId)
            .NotEmpty().WithMessage("Selecciona la tarjeta de crédito utilizada")
            .When(x => x.PaymentMethod == "credit_card");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("La clave de idempotencia es obligatoria para compras con tarjeta")
            .When(x => x.PaymentMethod == "credit_card");

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

        RuleFor(x => x.PaymentMethod)
            .Must(m => ValidPaymentMethods.Contains(m))
            .WithMessage($"Método de pago inválido. Valores permitidos: {string.Join(", ", ValidPaymentMethods)}");


        // Si es recurrente, debe tener tipo de recurrencia
        RuleFor(x => x.RecurrenceType)
            .NotEmpty()
            .WithMessage("El tipo de recurrencia es requerido cuando el gasto es recurrente")
            .Must(r => ValidRecurrenceTypes.Contains(r!))
            .WithMessage($"Tipo de recurrencia inválido. Valores: {string.Join(", ", ValidRecurrenceTypes)}")
            .When(x => x.IsRecurring);

        // Si no es recurrente, no debe tener tipo de recurrencia
        RuleFor(x => x.RecurrenceType)
            .Null()
            .WithMessage("El tipo de recurrencia solo aplica para gastos recurrentes")
            .When(x => !x.IsRecurring);

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Las notas no pueden superar 1000 caracteres")
            .When(x => x.Notes != null);

        RuleFor(x => x.Merchant)
            .MaximumLength(200)
            .When(x => x.Merchant != null);

        RuleFor(x => x.TagIds)
            .Must(ids => ids.Distinct().Count() <= 10)
            .WithMessage("Un gasto puede tener como máximo 10 etiquetas");
    }
}