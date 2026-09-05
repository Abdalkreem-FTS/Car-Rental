using CarRental.Domain.Common;

namespace CarRental.Domain.Errors;

public static class IdempotencyErrors
{
    public static Error KeyRequired(string header, int maxLength) => Error.Validation(
        "idempotency.key_required",
        header,
        $"This endpoint needs an '{header}' header of 1 to {maxLength} characters, unique to the attempt.");

    public static Error KeyReused => Error.Conflict(
        "idempotency.key_reused",
        "This idempotency key was already used for a different request. Use a new key.");

    public static Error InProgress => Error.Conflict(
        "idempotency.in_progress",
        "An earlier request with this idempotency key is still being processed. Try again in a moment.");
}
