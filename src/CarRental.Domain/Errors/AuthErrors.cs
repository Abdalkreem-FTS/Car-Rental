using CarRental.Domain.Common;

namespace CarRental.Domain.Errors;

public static class AuthErrors
{
    public static Error InvalidRefreshToken => Error.Unauthorized(
        "auth.invalid_refresh_token",
        "Your session has expired. Please sign in again.");

    public static Error InvalidResetToken => Error.Validation(
        "token",
        "This password reset link is invalid or has expired. Please request a new one.");

    public static Error NotAuthenticated => Error.Unauthorized(
        "auth.not_authenticated",
        "You must be signed in to do that.");
}
