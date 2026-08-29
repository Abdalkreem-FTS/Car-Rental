using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Auth;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Identity;

namespace CarRental.Application.Services;

public sealed class SessionService(
    UserManager<ApplicationUser> userManager,
    ITokenIssuer tokenIssuer) : ISessionService
{
    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null)
        {
            VerifyAgainstNobody(request.Password);

            return UserErrors.InvalidCredentials;
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);

            return UserErrors.InvalidCredentials;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return UserErrors.LockedOut;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return await tokenIssuer.IssueAsync(user, cancellationToken);
    }

    public Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
        tokenIssuer.RotateAsync(request.RefreshToken, cancellationToken);

    public Task<Result<Success>> LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken = default) =>
        tokenIssuer.RevokeAsync(userId, refreshToken, cancellationToken);

    public Task<Result<Success>> LogoutEverywhereAsync(Guid userId, CancellationToken cancellationToken = default) =>
        tokenIssuer.RevokeEverythingAsync(userId, cancellationToken);

    private static readonly ApplicationUser Nobody = new()
    {
        FirstName = string.Empty,
        LastName = string.Empty,
        AddressLine1 = string.Empty,
        City = string.Empty,
        Country = string.Empty,
        DriverLicenseNumber = string.Empty,
    };

    private static string? _nobodysHash;

    private void VerifyAgainstNobody(string password)
    {
        _nobodysHash ??= userManager.PasswordHasher.HashPassword(Nobody, Guid.NewGuid().ToString());

        userManager.PasswordHasher.VerifyHashedPassword(Nobody, _nobodysHash, password);
    }
}
