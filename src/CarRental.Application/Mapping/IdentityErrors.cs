using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Identity;

namespace CarRental.Application.Mapping;

public static class IdentityErrors
{
    public const string DuplicateUserName = nameof(DuplicateUserName);
    public const string DuplicateEmail = nameof(DuplicateEmail);
    public const string InvalidUserName = nameof(InvalidUserName);
    public const string InvalidEmail = nameof(InvalidEmail);
    public const string PasswordMismatch = nameof(PasswordMismatch);
    public const string InvalidToken = nameof(InvalidToken);

    private static readonly string[] PasswordRules =
    [
        "PasswordTooShort",
        "PasswordRequiresUniqueChars",
        "PasswordRequiresNonAlphanumeric",
        "PasswordRequiresDigit",
        "PasswordRequiresLower",
        "PasswordRequiresUpper",
    ];

    public static List<Error> Map(IdentityResult result, string passwordField, Error fallback) =>
        [.. result.Errors.Select(error => ToError(error, passwordField, fallback))];

    public static bool Contains(this IdentityResult result, string code) =>
        result.Errors.Any(error => string.Equals(error.Code, code, StringComparison.Ordinal));

    private static Error ToError(IdentityError error, string passwordField, Error fallback)
    {
        if (PasswordRules.Contains(error.Code, StringComparer.Ordinal))
        {
            return Error.Validation(passwordField, error.Description);
        }

        return error.Code switch
        {
            DuplicateUserName or DuplicateEmail => UserErrors.EmailAlreadyInUse(),
            InvalidUserName or InvalidEmail => Error.Validation("email", error.Description),
            PasswordMismatch => UserErrors.IncorrectPassword,
            _ => fallback,
        };
    }
}
