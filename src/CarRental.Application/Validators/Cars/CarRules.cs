using FluentValidation;

namespace CarRental.Application.Validators.Cars;

internal static class CarRules
{
    internal static IRuleBuilderOptions<T, string> ValidCarText<T>(this IRuleBuilder<T, string> rule, string label, int maxLength) =>
        rule.NotEmpty().WithMessage($"{label} is required.")
            .MaximumLength(maxLength).WithMessage($"{label} cannot exceed {maxLength} characters.");

    internal static IRuleBuilderOptions<T, string> ValidPlateNumber<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Plate number is required.")
            .Matches("^[a-zA-Z0-9 -]{2,20}$")
            .WithMessage("Enter a valid plate number (2-20 letters, digits, spaces or hyphens).");

    internal static IRuleBuilderOptions<T, int> ValidCarYear<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1950, DateTimeOffset.UtcNow.Year + 1)
            .WithMessage($"Year must be between 1950 and {DateTimeOffset.UtcNow.Year + 1}.");

    internal static IRuleBuilderOptions<T, decimal> ValidDailyRate<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0).WithMessage("Daily rate must be greater than zero.")
            .LessThanOrEqualTo(100_000).WithMessage("Daily rate is unrealistically high.");

    internal static IRuleBuilderOptions<T, int> ValidSeats<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, 20).WithMessage("Seats must be between 1 and 20.");
}
