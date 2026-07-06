using FinanceApp.Application.DTOs.Auth;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;

    // Monedas soportadas — en v2 podemos cargar esto desde BD o config
    private static readonly string[] SupportedCurrencies =
        { "USD", "EUR", "COP", "PEN", "MXN", "ARS", "CLP", "BRL" };

    public ProfileService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserInfoDto> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
            throw new NotFoundException("Usuario", userId);

        return MapToDto(user);
    }

    public async Task<UserInfoDto> UpdateProfileAsync(
        Guid userId,
        ProfileUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
            throw new NotFoundException("Usuario", userId);

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();

        await _userRepository.UpdateAsync(user, cancellationToken);
        return MapToDto(user);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
            throw new NotFoundException("Usuario", userId);

        // Verificar que la contraseña actual es correcta
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new DomainException(
                "INVALID_CURRENT_PASSWORD",
                "La contraseña actual es incorrecta");

        // Verificar que la nueva contraseña no sea igual a la actual
        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
            throw new DomainException(
                "SAME_PASSWORD",
                "La nueva contraseña debe ser diferente a la actual");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task<UserInfoDto> ChangeCurrencyAsync(
        Guid userId,
        ChangeCurrencyDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!SupportedCurrencies.Contains(dto.CurrencyCode.ToUpper()))
            throw new DomainException(
                "UNSUPPORTED_CURRENCY",
                $"Moneda no soportada. Valores permitidos: {string.Join(", ", SupportedCurrencies)}");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null || user.IsDeleted)
            throw new NotFoundException("Usuario", userId);

        user.CurrencyCode = dto.CurrencyCode.ToUpper();
        await _userRepository.UpdateAsync(user, cancellationToken);
        return MapToDto(user);
    }

    private static UserInfoDto MapToDto(Domain.Entities.User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        CurrencyCode = user.CurrencyCode
    };
}