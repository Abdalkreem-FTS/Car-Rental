using FluentValidation;

namespace CarRental.Application.Validators.Reservations;

internal static class ReservationRules
{
    private const int MaxRentalDays = 90;

    internal static IRuleBuilderOptions<T, DateOnly> ValidPickupDate<T>(this IRuleBuilder<T, DateOnly> rule) =>
        rule.GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime))
            .WithMessage("The pickup date cannot be in the past.");

    internal static IRuleBuilderOptions<T, DateOnly> ValidReturnDate<T>(
        this IRuleBuilder<T, DateOnly> rule,
        Func<T, DateOnly> startDate) =>
        rule.GreaterThanOrEqualTo(request => startDate(request))
            .WithMessage("The return date must be on or after the pickup date.")
            .Must((request, endDate) => endDate.DayNumber - startDate(request).DayNumber + 1 <= MaxRentalDays)
            .WithMessage($"A single rental cannot exceed {MaxRentalDays} days.");

    internal static IRuleBuilderOptions<T, string?> ValidPickupLocation<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(120).WithMessage("Pickup location cannot exceed 120 characters.");
}
