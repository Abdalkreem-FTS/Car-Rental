using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Profile;
using CarRental.Application.Mapping;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Identity;

namespace CarRental.Application.Services;

public sealed class ProfileService(
    UserManager<ApplicationUser> userManager,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork) : IProfileService
{
    public async Task<Result<ProfileResponse>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return UserErrors.NotFound;
        }

        return user.ToProfileResponse((await userManager.GetRolesAsync(user)).ToList());
    }

    public async Task<Result<ProfileResponse>> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return UserErrors.NotFound;
        }

        var driverLicenseNumber = request.DriverLicenseNumber.Trim().ToUpperInvariant();

        if (userManager.Users.Any(other => other.DriverLicenseNumber == driverLicenseNumber && other.Id != userId))
        {
            return UserErrors.LicenseAlreadyInUse;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.DateOfBirth = request.DateOfBirth;
        user.AddressLine1 = request.AddressLine1.Trim();
        user.AddressLine2 = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2.Trim();
        user.City = request.City.Trim();
        user.Country = request.Country.Trim();
        user.DriverLicenseNumber = driverLicenseNumber;

        var updated = await userManager.UpdateAsync(user);

        if (!updated.Succeeded)
        {
            return MapIdentityErrors(updated);
        }

        return user.ToProfileResponse((await userManager.GetRolesAsync(user)).ToList());
    }

    public async Task<Result<Updated>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return UserErrors.NotFound;
        }

        var changed = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!changed.Succeeded)
        {
            return changed.Errors.Any(error => error.Code == "PasswordMismatch")
                ? UserErrors.IncorrectPassword
                : MapIdentityErrors(changed);
        }
        
        await refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    private static List<Error> MapIdentityErrors(IdentityResult result) =>
    [
        .. result.Errors.Select(error => error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
            ? Error.Validation("newPassword", error.Description)
            : Error.Failure("user.update_failed", error.Description)),
    ];
}
