using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarRental.Infrastructure.Persistence.Stores;

public sealed class IdempotencyStore(AppDbContext context) : IIdempotencyStore
{
    public async Task<IdempotencyClaim> ClaimAsync(
        Guid userId,
        string endpoint,
        string key,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        context.IdempotentRequests.Add(new IdempotentRequest
        {
            UserId = userId,
            Endpoint = endpoint,
            Key = key,
            RequestHash = requestHash,
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return new IdempotencyClaim(ClaimOutcome.Claimed, null);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            context.ChangeTracker.Clear();

            var existing = await FindAsync(userId, endpoint, key, cancellationToken);

            if (existing is null)
            {
                return new IdempotencyClaim(ClaimOutcome.InProgress, null);
            }

            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                return new IdempotencyClaim(ClaimOutcome.KeyReused, existing);
            }

            return existing.CompletedAtUtc is null
                ? new IdempotencyClaim(ClaimOutcome.InProgress, existing)
                : new IdempotencyClaim(ClaimOutcome.Replay, existing);
        }
    }

    public async Task CompleteAsync(
        Guid userId,
        string endpoint,
        string key,
        int statusCode,
        string? responseBody,
        CancellationToken cancellationToken = default)
    {
        if (await FindAsync(userId, endpoint, key, cancellationToken) is not { } request)
        {
            return;
        }

        request.StatusCode = statusCode;
        request.ResponseBody = responseBody;
        request.CompletedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(Guid userId, string endpoint, string key, CancellationToken cancellationToken = default)
    {
        await context.IdempotentRequests
            .Where(request =>
                request.UserId == userId &&
                request.Endpoint == endpoint &&
                request.Key == key &&
                request.CompletedAtUtc == null)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Task<IdempotentRequest?> FindAsync(Guid userId, string endpoint, string key, CancellationToken cancellationToken) =>
        context.IdempotentRequests.FirstOrDefaultAsync(
            request => request.UserId == userId && request.Endpoint == endpoint && request.Key == key,
            cancellationToken);

    private static bool IsDuplicateKey(Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            if (exception is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }
        }

        return false;
    }
}
