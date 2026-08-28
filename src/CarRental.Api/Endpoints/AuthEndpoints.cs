using System.Security.Claims;
using CarRental.Api.Extensions;
using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Auth;
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
                IAuthService authService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.RegisterAsync(request, cancellationToken);

                return result.ToOkWithRefreshCookie(context);
            })
            .WithValidation<RegisterRequest>()
            .RequireRateLimiting(RateLimiting.Accounts)
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Create a customer account and sign in immediately.");

        group.MapPost("/login", async (
                LoginRequest request,
                IAuthService authService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.LoginAsync(request, cancellationToken);

                return result.ToOkWithRefreshCookie(context);
            })
            .WithValidation<LoginRequest>()
            .RequireRateLimiting(RateLimiting.Accounts)
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Exchange email and password for an access token and a refresh token.");

        group.MapPost("/refresh", async (
                IAuthService authService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                if (RefreshTokenCookie.Read(context) is not { } presented)
                {
                    return AuthErrors.InvalidRefreshToken.ToProblem();
                }

                var result = await authService.RefreshAsync(new RefreshTokenRequest(presented), cancellationToken);

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
                IAuthService authService,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.LogoutAsync(user.GetUserId(), cancellationToken);

                RefreshTokenCookie.Clear(context);

                return result.ToNoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Revoke every refresh token for the signed-in user.");

        group.MapPost("/confirm-email", async (
                ConfirmEmailRequest request,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.ConfirmEmailAsync(request, cancellationToken);

                return result.ToNoContent();
            })
            .WithValidation<ConfirmEmailRequest>()
            .RequireRateLimiting(RateLimiting.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Confirm an email address using the token from the confirmation link.");

        group.MapPost("/resend-confirmation", async (
                ResendConfirmationRequest request,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.ResendConfirmationAsync(request, cancellationToken);

                return result.ToAccepted();
            })
            .WithValidation<ResendConfirmationRequest>()
            .RequireRateLimiting(RateLimiting.Auth)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Send the confirmation link again. Always reports success, confirmed or not.");

        group.MapPost("/forgot-password", async (
                ForgotPasswordRequest request,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.ForgotPasswordAsync(request, cancellationToken);

                return result.ToAccepted();
            })
            .WithValidation<ForgotPasswordRequest>()
            .RequireRateLimiting(RateLimiting.Auth)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Email a password reset link. Always reports success, registered or not.");

        group.MapPost("/reset-password", async (
                ResetPasswordRequest request,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.ResetPasswordAsync(request, cancellationToken);

                return result.ToNoContent();
            })
            .WithValidation<ResetPasswordRequest>()
            .RequireRateLimiting(RateLimiting.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Set a new password using the token from the reset link.");

        return app;
    }
}
