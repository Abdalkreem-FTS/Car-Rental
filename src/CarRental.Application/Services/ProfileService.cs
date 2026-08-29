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
    IUserAccountStore userAccounts,
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

        request.ApplyTo(user);

        var updated = await userAccounts.UpdateAsync(user, cancellationToken);

        if (updated.IsError)
        {
            return updated.Errors;
        }

        if (!updated.Value.Succeeded)
        {
            return IdentityErrors.Map(updated.Value, "newPassword", UserErrors.UpdateFailed);
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
            return IdentityErrors.Map(changed, "newPassword", UserErrors.UpdateFailed);
        }
        
        await refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
