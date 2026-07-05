using FinanceApp.Application.DTOs.Auth;
using FluentValidation;

namespace FinanceApp.Application.Validators;

/// <summary>
/// Valida los datos de registro antes de procesarlos.
/// FluentValidation permite escribir reglas de forma expresiva y legible.
/// </summary>
public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido")
            .MaximumLength(100).WithMessage("El apellido no puede superar 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido")
            .EmailAddress().WithMessage("El email no tiene un formato válido")
            .MaximumLength(255).WithMessage("El email no puede superar 255 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos una mayúscula")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("La confirmación de contraseña es requerida")
            .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden");
    }
}