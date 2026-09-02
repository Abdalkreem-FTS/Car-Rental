using CarRental.Domain.Rules;
using CarRental.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace CarRental.Application.Validators;

public static class SharedRules
{
    public const int MinimumRenterAge = RenterRules.MinimumAge;

    public const int MaximumPasswordLength = 128;

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(
        this IRuleBuilder<T, string> rule,
        PasswordOptions policy)
    {
        var built = rule.NotEmpty().WithMessage("Password is required.")
            .MinimumLength(policy.RequiredLength)
            .WithMessage($"Password must be at least {policy.RequiredLength} characters long.")
            .MaximumLength(MaximumPasswordLength)
            .WithMessage($"Password cannot exceed {MaximumPasswordLength} characters.");

        if (policy.RequireUppercase)
        {
            built = built.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.");
        }

        if (policy.RequireLowercase)
        {
            built = built.Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.");
        }

        if (policy.RequireDigit)
        {
            built = built.Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        }

        if (policy.RequireNonAlphanumeric)
        {
            built = built.Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        }

        return built;
    }

    public static IRuleBuilderOptions<T, string> ValidPhoneNumber<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Phone number is required.")
            .Must(number => PhoneNumbers.TryNormalise(number, out _))
            .WithMessage("Enter a phone number in international format, like +962791234567.");

    public static IRuleBuilderOptions<T, string> ValidDriverLicense<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Driver's license number is required.")
            .MaximumLength(30).WithMessage("Driver's license number cannot exceed 30 characters.");

    public static IRuleBuilderOptions<T, DateOnly?> ValidDateOfBirth<T>(this IRuleBuilder<T, DateOnly?> rule) =>
        rule.Must(date => date is null || date.Value < DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime))
            .WithMessage("Date of birth cannot be in the future.")
            .Must(date => date is null || RenterRules.AgeOn(date.Value, DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime)) >= MinimumRenterAge)
            .WithMessage($"You must be at least {MinimumRenterAge} years old to rent a car.");

    public const int MinimumSearchTermLength = 3;

    public static IRuleBuilderOptions<T, string?> ValidSearchTerm<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(term => string.IsNullOrWhiteSpace(term) || term.Trim().Length >= MinimumSearchTermLength)
            .WithMessage($"Search for at least {MinimumSearchTermLength} characters.");

    public static readonly string[] CarSortKeys = ["price_asc", "price_desc", "year_desc", "seats_desc"];

    public static IRuleBuilderOptions<T, string?> ValidCarSortKey<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(key => string.IsNullOrWhiteSpace(key) || CarSortKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Sort by one of: {string.Join(", ", CarSortKeys)}.");
}
