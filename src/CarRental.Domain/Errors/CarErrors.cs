using CarRental.Domain.Common;

namespace CarRental.Domain.Errors;

public static class CarErrors
{
    public static Error NotFound => Error.NotFound(
        "car.not_found",
        "We could not find that car.");

    public static Error PlateAlreadyInUse(string? plate = null) => Error.Conflict(
        "car.plate_already_in_use",
        plate is null
            ? "A car with that plate number already exists."
            : $"A car with plate number '{plate}' already exists.");

    public static Error Unavailable => Error.Conflict(
        "car.unavailable",
        "This car is already booked for one or more of the days you selected.");

    public static Error InvalidFilters(string reason) => Error.Validation(
        "car.invalid_filters",
        "filters",
        reason);

    public static Error InvalidSorts(string reason) => Error.Validation(
        "car.invalid_sorts",
        "sorts",
        reason);

    public static Error HasActiveBookings(int count) => Error.Conflict(
        "car.has_active_bookings",
        $"This car has {count} confirmed booking(s) that have not finished. Retire it anyway to cancel them, or wait until they are over.");
}
