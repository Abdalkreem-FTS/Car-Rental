using CarRental.Domain;
using FluentValidation;

namespace CarRental.Application.Validators;

public static class SharedRules
{
    public const int MinimumRenterAge = RenterRules.MinimumAge;

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

    public static IRuleBuilderOptions<T, string> ValidPhoneNumber<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9\s\-()]{7,20}$")
            .WithMessage("Enter a valid phone number (7-20 digits, optionally starting with +).");

    public static IRuleBuilderOptions<T, string> ValidDriverLicense<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Driver's license number is required.")
            .Matches("^[a-zA-Z0-9-]{5,30}$")
            .WithMessage("Enter a valid driver's license number (5-30 letters, digits or hyphens).");

    public static IRuleBuilderOptions<T, DateOnly?> ValidDateOfBirth<T>(this IRuleBuilder<T, DateOnly?> rule) =>
        rule.Must(date => date is null || date.Value < DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime))
            .WithMessage("Date of birth cannot be in the future.")
            .Must(date => date is null || RenterRules.AgeOn(date.Value, DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime)) >= MinimumRenterAge)
            .WithMessage($"You must be at least {MinimumRenterAge} years old to rent a car.");

}
