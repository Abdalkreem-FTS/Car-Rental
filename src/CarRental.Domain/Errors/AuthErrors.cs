using CarRental.Domain.Common;

namespace CarRental.Domain.Errors;

public static class AuthErrors
{
    public static Error InvalidRefreshToken => Error.Unauthorized(
        "auth.invalid_refresh_token",
        "Your session has expired. Please sign in again.");

    public static Error InvalidResetToken => Error.Validation(
        "auth.invalid_reset_token",
        "token",
        "This password reset link is invalid or has expired. Please request a new one.");

    public static Error NotAuthenticated => Error.Unauthorized(
        "auth.not_authenticated",
        "You must be signed in to do that.");

    public static Error InvalidConfirmationToken => Error.Validation(
        "auth.invalid_confirmation_token",
        "token",
        "This confirmation link is invalid or has expired. Please request a new one.");

    public static Error RefreshTokenReused => Error.Unauthorized(
        "auth.refresh_token_reused",
        "This session has been signed out because a refresh token was presented twice. Please sign in again.");
}
