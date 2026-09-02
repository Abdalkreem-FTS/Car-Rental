using System.Security.Claims;
using CarRental.Api.Contracts.Auth;
using CarRental.Api.Errors;
using CarRental.Api.Filters;
using CarRental.Api.Http;
using CarRental.Api.Mapping;
using CarRental.Api.RateLimiting;
using CarRental.Api.Security;
using CarRental.Application.Abstractions;
using CarRental.Application.Dtos.Auth;
using CarRental.Domain.Errors;

namespace CarRental.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AllowAnonymous();

        var authenticated = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/register", async (
                RegisterRequest request,
                IRegistrationService registrationService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await registrationService.RegisterAsync(request.ToDto(), cancellationToken);

                return result.ToOkWithRefreshCookie(context);
            })
            .WithValidation<RegisterRequest, RegisterDto>()
            .RequireRateLimiting(RateLimitPolicies.Accounts)
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Create a customer account and sign in immediately.");

        group.MapPost("/login", async (
                LoginRequest request,
                ISessionService sessionService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await sessionService.LoginAsync(request.ToDto(), cancellationToken);

                return result.ToOkWithRefreshCookie(context);
            })
            .WithValidation<LoginRequest, LoginDto>()
            .RequireRateLimiting(RateLimitPolicies.Accounts)
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Exchange email and password for an access token and a refresh token.");

        group.MapPost("/refresh", async (
                ISessionService sessionService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                if (RefreshTokenCookie.Read(context) is not { } presented)
                {
                    return AuthErrors.InvalidRefreshToken.ToProblem();
                }

                var result = await sessionService.RefreshAsync(new RefreshTokenRequest(presented).ToDto(), cancellationToken);

                if (result.IsError)
                {
                    RefreshTokenCookie.Clear(context);
                }

                return result.ToOkWithRefreshCookie(context);
            })
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Trade a refresh token for a new pair. The presented token is revoked.");

        authenticated.MapPost("/logout", async (
                ClaimsPrincipal user,
                ISessionService sessionService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await sessionService.LogoutAsync(user.GetUserId(), RefreshTokenCookie.Read(context), cancellationToken);

                RefreshTokenCookie.Clear(context);

                return result.ToNoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Sign out of this device. Sessions on other devices keep working.");

        authenticated.MapPost("/logout-all", async (
                ClaimsPrincipal user,
                ISessionService sessionService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await sessionService.LogoutEverywhereAsync(user.GetUserId(), cancellationToken);

                RefreshTokenCookie.Clear(context);

                return result.ToNoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Sign out of every device. Use this if an account may be compromised.");

        group.MapPost("/confirm-email", async (
                ConfirmEmailRequest request,
                IRegistrationService registrationService,
                CancellationToken cancellationToken) =>
            {
                var result = await registrationService.ConfirmEmailAsync(request.ToDto(), cancellationToken);

                return result.ToNoContent();
            })
            .WithValidation<ConfirmEmailRequest, ConfirmEmailDto>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Confirm an email address using the token from the confirmation link.");

        group.MapPost("/resend-confirmation", async (
                ResendConfirmationRequest request,
                IRegistrationService registrationService,
                CancellationToken cancellationToken) =>
            {
                var result = await registrationService.ResendConfirmationAsync(request.ToDto(), cancellationToken);

                return result.ToAccepted();
            })
            .WithValidation<ResendConfirmationRequest, ResendConfirmationDto>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Send the confirmation link again. Always reports success, confirmed or not.");

        group.MapPost("/forgot-password", async (
                ForgotPasswordRequest request,
                IPasswordResetService passwordResetService,
                CancellationToken cancellationToken) =>
            {
                var result = await passwordResetService.ForgotPasswordAsync(request.ToDto(), cancellationToken);

                return result.ToAccepted();
            })
            .WithValidation<ForgotPasswordRequest, ForgotPasswordDto>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Email a password reset link. Always reports success, registered or not.");

        group.MapPost("/reset-password", async (
                ResetPasswordRequest request,
                IPasswordResetService passwordResetService,
                CancellationToken cancellationToken) =>
            {
                var result = await passwordResetService.ResetPasswordAsync(request.ToDto(), cancellationToken);

                return result.ToNoContent();
            })
            .WithValidation<ResetPasswordRequest, ResetPasswordDto>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Set a new password using the token from the reset link.");

        return app;
    }
}
