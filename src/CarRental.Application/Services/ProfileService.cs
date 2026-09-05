using CarRental.Application.Abstractions;
using CarRental.Application.Dtos.Profile;
using CarRental.Application.Mapping;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CarRental.Application.Services;

public sealed class ProfileService(
    UserManager<ApplicationUser> userManager,
    IUserAccountStore userAccounts,
    IRefreshTokenRepository refreshTokens,
    IReservationRepository reservations,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<ProfileService> logger) : IProfileService
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public async Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await userAccounts.GetWithRolesAsync(userId, cancellationToken) is not { } profile)
        {
            return UserErrors.NotFound;
        }

        return profile.User.ToProfileDto(profile.Roles);
    }

    public async Task<Result<ProfileDto>> UpdateAsync(Guid userId, UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return UserErrors.NotFound;
        }

        var requestedLicence = ApplicationUser.NormaliseLicence(request.DriverLicenseNumber);

        if (!string.Equals(requestedLicence, user.DriverLicenseNumber, StringComparison.Ordinal)
            && await reservations.HasUnfinishedForUserAsync(userId, Today, cancellationToken))
        {
            logger.LogWarning("Refused a licence change for {UserId} while a booking stands against the old one.", userId);

            return UserErrors.LicenceLockedByBooking;
        }

        user.Describe(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.DateOfBirth,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Country,
            request.DriverLicenseNumber);

        var updated = await userAccounts.UpdateAsync(user, cancellationToken);

        if (updated.IsError)
        {
            return updated.Errors;
        }

        if (!updated.Value.Succeeded)
        {
            return IdentityErrors.Map(updated.Value, "newPassword", UserErrors.UpdateFailed);
        }

        return user.ToProfileDto((await userManager.GetRolesAsync(user)).ToList());
    }

    public async Task<Result<Updated>> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return UserErrors.NotFound;
        }

        var changed = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!changed.Succeeded)
        {
            return IdentityErrors.Map(changed, "newPassword", UserErrors.UpdateFailed);
        }

        await refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
