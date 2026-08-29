using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public enum ClaimOutcome
{
    Claimed,
    Replay,
    KeyReused,
    InProgress,
}

public sealed record IdempotencyClaim(ClaimOutcome Outcome, IdempotentRequest? Existing);

public interface IIdempotencyStore
{
    Task<IdempotencyClaim> ClaimAsync(
        Guid userId,
        string endpoint,
        string key,
        string requestHash,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid userId,
        string endpoint,
        string key,
        int statusCode,
        string? responseBody,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(Guid userId, string endpoint, string key, CancellationToken cancellationToken = default);
}
