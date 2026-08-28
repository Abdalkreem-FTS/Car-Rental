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

        var created = await userAccounts.CreateAsync(user, request.Password, cancellationToken);

        if (created.IsError)
        {
            return created.Errors;
        }

        if (!created.Value.Succeeded)
        {
            return MapIdentityErrors(created.Value);
        }

        await userManager.AddToRoleAsync(user, Roles.Customer);

        logger.LogInformation("Registered new user {UserId}", user.Id);

        return await IssueTokensAsync(user, cancellationToken);
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

        if (stored?.User is null || !stored.IsActive(DateTimeOffset.UtcNow))
        {
            return AuthErrors.InvalidRefreshToken;
        }
        
        stored.RevokedAtUtc = DateTimeOffset.UtcNow;

        return await IssueTokensAsync(stored.User, cancellationToken);
    }

    public async Task<Result<Success>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await refreshTokens.RevokeAllForUserAsync(userId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success;
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

        var link = $"{_clientApp.BaseUrl.TrimEnd('/')}{_clientApp.ResetPasswordPath}" + $"?email={Uri.EscapeDataString(user.Email!)}&token={Encode(token)}";

        await emailSender.SendPasswordResetAsync(user.Email!, user.FirstName, link, cancellationToken);

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

    private async Task<Result<AuthResponse>> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (accessToken, expiresAtUtc) = tokenGenerator.GenerateAccessToken(user, roles);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = tokenGenerator.GenerateRefreshToken(),
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(tokenGenerator.RefreshTokenLifetime),
        };

        refreshTokens.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken.Token, expiresAtUtc, user.ToResponse(roles));
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
