using System.Security.Claims;
using CarRental.Api.Extensions;
using CarRental.Application.Abstractions;
using CarRental.Application.Contracts.Auth;

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
                CancellationToken cancellationToken) =>
            {
                var result = await authService.RegisterAsync(request, cancellationToken);

                return result.ToCreated(_ => "/api/profile");
            })
            .WithValidation<RegisterRequest>()
            .RequireRateLimiting(RateLimiting.Accounts)
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Create a customer account and sign in immediately.");

        group.MapPost("/login", async (
                LoginRequest request,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.LoginAsync(request, cancellationToken);

                return result.ToOk();
            })
            .WithValidation<LoginRequest>()
            .RequireRateLimiting(RateLimiting.Accounts)
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Exchange email and password for an access token and a refresh token.");

        group.MapPost("/refresh", async (
                RefreshTokenRequest request,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.RefreshAsync(request, cancellationToken);

                return result.ToOk();
            })
            .WithValidation<RefreshTokenRequest>()
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Trade a refresh token for a new pair. The presented token is revoked.");

        authenticated.MapPost("/logout", async (
                ClaimsPrincipal user,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.LogoutAsync(user.GetUserId(), cancellationToken);

                return result.ToNoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Revoke every refresh token for the signed-in user.");

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
