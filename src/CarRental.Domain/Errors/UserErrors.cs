using CarRental.Domain.Common;

namespace CarRental.Domain.Errors;

public static class UserErrors
{
    public static Error EmailAlreadyInUse(string? email = null) => Error.Conflict(
        "user.email_already_in_use",
        email is null
            ? "An account with that email already exists."
            : $"An account with the email '{email}' already exists.");

    public static Error LicenseAlreadyInUse => Error.Conflict(
        "user.license_already_in_use",
        "An account with that driver's license number already exists.");

    public static Error NotFound => Error.NotFound(
        "user.not_found",
        "We could not find that account.");

    /// <summary>
    /// Deliberately identical for a wrong password and an unknown email so the response
    /// cannot be used to discover which addresses are registered.
    /// </summary>
    public static Error InvalidCredentials => Error.Unauthorized(
        "user.invalid_credentials",
        "The email or password you entered is incorrect.");

    public static Error LockedOut => Error.Forbidden(
        "user.locked_out",
        "This account is temporarily locked after too many failed sign-in attempts. Please try again later.");

    public static Error IncorrectPassword => Error.Validation(
        "user.incorrect_password",
        "currentPassword",
        "Your current password is incorrect.");

    public static Error EmailNotConfirmed => Error.Forbidden(
        "user.email_not_confirmed",
        "Please confirm your email address before booking a car. Check your inbox for the confirmation link.");

    public static Error RegistrationIncomplete => Error.Failure(
        "user.registration_incomplete",
        "We could not finish setting up the account. Nothing was saved. Please try again.");

    public static Error RegistrationFailed => Error.Failure(
        "user.registration_failed",
        "We could not create the account. Please try again.");

    public static Error UpdateFailed => Error.Failure(
        "user.update_failed",
        "We could not save your details. Please try again.");
}
