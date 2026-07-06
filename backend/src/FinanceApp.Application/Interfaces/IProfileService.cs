using FinanceApp.Application.DTOs.Auth;

namespace FinanceApp.Application.Interfaces;

public interface IProfileService
{
    Task<UserInfoDto> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserInfoDto> UpdateProfileAsync(
        Guid userId,
        ProfileUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordDto dto,
        CancellationToken cancellationToken = default);

    Task<UserInfoDto> ChangeCurrencyAsync(
        Guid userId,
        ChangeCurrencyDto dto,
        CancellationToken cancellationToken = default);
}