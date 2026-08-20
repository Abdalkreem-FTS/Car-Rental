using CarRental.Domain.Common;

namespace CarRental.Domain.Errors;

public static class ReservationErrors
{
    public static Error NotFound => Error.NotFound(
        "reservation.not_found",
        "We could not find that reservation.");

    public static Error NotYours => Error.Forbidden(
        "reservation.not_yours",
        "This reservation belongs to another account.");

    public static Error AlreadyCancelled => Error.Conflict(
        "reservation.already_cancelled",
        "This reservation has already been cancelled.");

    public static Error AlreadyStarted => Error.Conflict(
        "reservation.already_started",
        "A rental that has already started cannot be cancelled.");
}
