using System.Buffers.Text;
using System.Text;
using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Auth;
using CarRental.Application.Mapping;
using CarRental.Application.Options;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarRental.Application.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IUserAccountStore userAccounts,
    IJwtTokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    IOptions<ClientAppOptions> clientApp,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly ClientAppOptions _clientApp = clientApp.Value;

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return UserErrors.EmailAlreadyInUse(email);
        }

        var driverLicenseNumber = request.DriverLicenseNumber.Trim().ToUpperInvariant();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            DriverLicenseNumber = driverLicenseNumber,
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var created = await userAccounts.CreateAsync(user, request.Password, cancellationToken);

        if (created.IsError)
        {
            return created.Errors;
        }

        if (!created.Value.Succeeded)
        {
            return MapIdentityErrors(created.Value);
        }

        var role = await userManager.AddToRoleAsync(user, Roles.Customer);

        if (!role.Succeeded)
        {
            logger.LogError(
                "Could not put {UserId} in the {Role} role: {Errors}",
                user.Id,
                Roles.Customer,
                string.Join("; ", role.Errors.Select(error => error.Description)));

            return UserErrors.RegistrationIncomplete;
        }

        var tokens = await IssueTokensAsync(user, cancellationToken);

        if (tokens.IsError)
        {
            return tokens.Errors;
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Registered new user {UserId}", user.Id);

        await SendConfirmationAsync(user, cancellationToken);

        return tokens;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        
        if (user is null)
        {
            return UserErrors.InvalidCredentials;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return UserErrors.LockedOut;
        }
        
        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);

            return await userManager.IsLockedOutAsync(user)
                ? UserErrors.LockedOut
                : UserErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (stored is null)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        // Asked for explicitly rather than relying on the repository having included the
        // navigation, so a query the repository changes later cannot quietly turn this into an
        // authentication hole.
        if (await userManager.FindByIdAsync(stored.UserId.ToString()) is not { } user)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        var replacement = NewRefreshToken(stored.UserId);

        if (await refreshTokens.TrySpendAsync(request.RefreshToken, replacement.Entity.Id, cancellationToken))
        {
            return await IssueTokensAsync(user, replacement, cancellationToken);
        }

        return await RefusalFor(request.RefreshToken, stored.UserId, cancellationToken);
    }

    private async Task<Result<AuthResponse>> RefusalFor(string token, Guid userId, CancellationToken cancellationToken)
    {
        var current = await refreshTokens.GetByTokenAsync(token, cancellationToken);

        if (current is not { WasSpent: true }
            || DateTimeOffset.UtcNow - current.RevokedAtUtc!.Value <= tokenGenerator.RefreshReuseLeeway)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        logger.LogWarning("Refresh token replayed for {UserId}. Revoking every session.", userId);

        await refreshTokens.RevokeAllForUserAsync(userId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthErrors.RefreshTokenReused;
    }

    public async Task<Result<Success>> LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Success;
        }

        await refreshTokens.RevokeAsync(refreshToken, userId, cancellationToken);

        return Result.Success;
    }

    public async Task<Result<Success>> LogoutEverywhereAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await refreshTokens.RevokeAllForUserAsync(userId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    public async Task<Result<Success>> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !TryDecode(request.Token, out var token))
        {
            return AuthErrors.InvalidConfirmationToken;
        }

        if (user.EmailConfirmed)
        {
            return Result.Success;
        }

        var confirmed = await userManager.ConfirmEmailAsync(user, token);

        return confirmed.Succeeded ? Result.Success : AuthErrors.InvalidConfirmationToken;
    }

    public async Task<Result<Success>> ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is not { EmailConfirmed: false })
        {
            logger.LogInformation("Confirmation resend requested for an address that cannot use one.");

            return Result.Success;
        }

        await SendConfirmationAsync(user, cancellationToken);

        return Result.Success;
    }

    private async Task SendConfirmationAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var link = _clientApp.LinkTo(_clientApp.ConfirmEmailPath, user.Email!, Encode(token));

        await emailSender.SendEmailConfirmationAsync(user.Email!, user.FirstName, link.ToString(), cancellationToken);
    }

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

        var link = _clientApp.LinkTo(_clientApp.ResetPasswordPath, user.Email!, Encode(token));

        await emailSender.SendPasswordResetAsync(user.Email!, user.FirstName, link.ToString(), cancellationToken);

        return Result.Success;
    }

    public async Task<Result<Success>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !TryDecode(request.Token, out var token))
        {
            return AuthErrors.InvalidResetToken;
        }

        var reset = await userManager.ResetPasswordAsync(user, token, request.Password);

        if (!reset.Succeeded)
        {
            return reset.Errors.Any(error => error.Code == "InvalidToken")
                ? AuthErrors.InvalidResetToken
                : MapIdentityErrors(reset);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);

        await refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    private static string Encode(string token) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token));

    private static bool TryDecode(string encoded, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(encoded));

            return true;
        }
        catch (FormatException)
        {
            token = string.Empty;

            return false;
        }
    }

    private (RefreshToken Entity, string Token) NewRefreshToken(Guid userId)
    {
        var token = tokenGenerator.GenerateRefreshToken();

        return (new RefreshToken
        {
            UserId = userId,
            TokenHash = RefreshToken.HashOf(token),
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(tokenGenerator.RefreshTokenLifetime),
        }, token);
    }

    private Task<Result<AuthResponse>> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        IssueTokensAsync(user, NewRefreshToken(user.Id), cancellationToken);

    private async Task<Result<AuthResponse>> IssueTokensAsync(
        ApplicationUser user,
        (RefreshToken Entity, string Token) refreshToken,
        CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (accessToken, expiresAtUtc) = tokenGenerator.GenerateAccessToken(user, roles);

        refreshTokens.Add(refreshToken.Entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            refreshToken.Token,
            refreshToken.Entity.ExpiresAtUtc,
            expiresAtUtc,
            user.ToResponse(roles));
    }
    
    private static List<Error> MapIdentityErrors(IdentityResult result) =>
    [
        .. result.Errors.Select(error => error.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName" => UserErrors.EmailAlreadyInUse(),
            var code when code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                => Error.Validation("password", error.Description),
            var code when code.Contains("Email", StringComparison.OrdinalIgnoreCase)
                || code.Contains("UserName", StringComparison.OrdinalIgnoreCase)
                => Error.Validation("email", error.Description),
            _ => Error.Failure("user.registration_failed", error.Description),
        }),
    ];
}
