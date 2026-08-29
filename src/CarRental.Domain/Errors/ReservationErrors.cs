using CarRental.Domain.Common;

namespace CarRental.Domain.Errors;

public static class ReservationErrors
{
    public static Error NotFound => Error.NotFound(
        "reservation.not_found",
        "We could not find that reservation.");

    public static Error AlreadyCancelled => Error.Conflict(
        "reservation.already_cancelled",
        "This reservation has already been cancelled.");

    public static Error AlreadyStarted => Error.Conflict(
        "reservation.already_started",
        "A rental that has already started cannot be cancelled.");

    public static Error VersionRequired => Error.PreconditionRequired(
        "reservation.version_required",
        "Send the version you last read in an 'If-Match' header, so a change made elsewhere is not overwritten.");

    public static Error VersionStale => Error.PreconditionFailed(
        "reservation.version_stale",
        "This reservation changed since you read it. Reload it and try again.");
}
