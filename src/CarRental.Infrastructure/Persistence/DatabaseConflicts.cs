using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CarRental.Infrastructure.Persistence;

internal static class DatabaseConflicts
{
    private static readonly Dictionary<string, Error> ByConstraint = new(StringComparer.Ordinal)
    {
        ["ck_reservations_no_overlapping_confirmed_bookings"] = CarErrors.Unavailable,
        ["EmailIndex"] = UserErrors.EmailAlreadyInUse(),
        ["IX_AspNetUsers_DriverLicenseNumber"] = UserErrors.LicenseAlreadyInUse,
        ["IX_Cars_PlateNumber"] = CarErrors.PlateAlreadyInUse(),
    };

    internal static DatabaseConflict? ConflictFor(DbUpdateException exception)
    {
        var violation = PostgresCause(exception);

        if (violation is not { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ExclusionViolation })
        {
            return null;
        }

        return violation.ConstraintName is { } constraint && ByConstraint.TryGetValue(constraint, out var error)
            ? new DatabaseConflict(constraint, violation.SqlState, error)
            : null;
    }

    internal static Error Report(this ILogger logger, DatabaseConflict conflict, DbUpdateException exception)
    {
        logger.LogWarning(
            exception,
            "The database refused a write: constraint {Constraint} ({SqlState}) reported to the caller as {ErrorCode}.",
            conflict.Constraint,
            conflict.SqlState,
            conflict.Error.Code);

        return conflict.Error;
    }

    private static PostgresException? PostgresCause(Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            if (exception is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }
}
