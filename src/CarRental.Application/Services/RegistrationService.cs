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

public sealed class RegistrationService(
    UserManager<ApplicationUser> userManager,
    IUserAccountStore userAccounts,
    ITokenIssuer tokenIssuer,
    IEmailOutbox emailOutbox,
    IUnitOfWork unitOfWork,
    IOptions<ClientAppOptions> clientApp,
    ILogger<RegistrationService> logger) : IRegistrationService
{
    private readonly ClientAppOptions _clientApp = clientApp.Value;

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return UserErrors.EmailAlreadyInUse(email);
        }

        var user = ApplicationUser.Register(
            email,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.DateOfBirth,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Country,
            request.DriverLicenseNumber);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var created = await userAccounts.CreateAsync(user, request.Password, cancellationToken);

        if (created.IsError)
        {
            return created.Errors;
        }

        if (!created.Value.Succeeded)
        {
            return IdentityErrors.Map(created.Value, "password", UserErrors.RegistrationFailed);
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

        var tokens = await tokenIssuer.IssueAsync(user, cancellationToken);

        if (tokens.IsError)
        {
            return tokens.Errors;
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Registered new user {UserId}", user.Id);

        await SendConfirmationAsync(user, cancellationToken);

        return tokens;
    }

    public async Task<Result<Success>> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !CredentialTokens.TryDecode(request.Token, out var token))
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

        var link = _clientApp.LinkTo(_clientApp.ConfirmEmailPath, user.Email!, CredentialTokens.Encode(token));

        emailOutbox.Enqueue(OutboxEmailKind.EmailConfirmation, user.Email!, user.FirstName, link.ToString());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
