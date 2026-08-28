using CarRental.Domain.Common;

namespace CarRental.Domain.Errors;

public static class CarErrors
{
    public static Error NotFound => Error.NotFound(
        "car.not_found",
        "We could not find that car.");

    public static Error PlateAlreadyInUse(string plate) => Error.Conflict(
        "car.plate_already_in_use",
        $"A car with plate number '{plate}' already exists.");

    public static Error Unavailable => Error.Conflict(
        "car.unavailable",
        "This car is already booked for one or more of the days you selected.");

    public static Error Inactive => Error.Conflict(
        "car.inactive",
        "This car is not currently part of the rental fleet.");

    public static Error InvalidFilters(string reason) => Error.Validation(
        "filters",
        reason);

    public static Error InvalidSorts(string reason) => Error.Validation(
        "sorts",
        reason);
}
