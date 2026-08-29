using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Auth;
using CarRental.Application.Mapping;
using CarRental.Application.Options;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarRental.Application.Services;

public sealed class PasswordResetService(
    UserManager<ApplicationUser> userManager,
    IRefreshTokenRepository refreshTokens,
    IEmailOutbox emailOutbox,
    IUnitOfWork unitOfWork,
    IOptions<ClientAppOptions> clientApp,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    private readonly ClientAppOptions _clientApp = clientApp.Value;

    public async Task<Result<Success>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            logger.LogInformation("Password reset requested for an address with no account.");

            return Result.Success;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var link = _clientApp.LinkTo(_clientApp.ResetPasswordPath, user.Email!, CredentialTokens.Encode(token));

        emailOutbox.Enqueue(OutboxEmailKind.PasswordReset, user.Email!, user.FirstName, link.ToString());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    public async Task<Result<Success>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !CredentialTokens.TryDecode(request.Token, out var token))
        {
            return AuthErrors.InvalidResetToken;
        }

        var reset = await userManager.ResetPasswordAsync(user, token, request.Password);

        if (!reset.Succeeded)
        {
            return reset.Contains(IdentityErrors.InvalidToken)
                ? AuthErrors.InvalidResetToken
                : IdentityErrors.Map(reset, "password", UserErrors.UpdateFailed);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);

        await refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
