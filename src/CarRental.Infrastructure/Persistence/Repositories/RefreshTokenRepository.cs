using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = RefreshToken.HashOf(token);

        return context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == hash, cancellationToken);
    }

    public async Task<bool> TrySpendAsync(string token, Guid replacedByTokenId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var hash = RefreshToken.HashOf(token);

        var spent = await context.RefreshTokens
            .Where(refreshToken =>
                refreshToken.TokenHash == hash &&
                refreshToken.RevokedAtUtc == null &&
                refreshToken.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(refreshToken => refreshToken.RevokedAtUtc, now)
                    .SetProperty(refreshToken => refreshToken.ReplacedByTokenId, replacedByTokenId),
                cancellationToken);

        return spent == 1;
    }

    public Task RevokeAsync(string token, Guid userId, CancellationToken cancellationToken = default)
    {
        var hash = RefreshToken.HashOf(token);

        return context.RefreshTokens
            .Where(refreshToken =>
                refreshToken.TokenHash == hash &&
                refreshToken.UserId == userId &&
                refreshToken.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(refreshToken => refreshToken.RevokedAtUtc, DateTimeOffset.UtcNow),
                cancellationToken);
    }

    public void Add(RefreshToken refreshToken) => context.RefreshTokens.Add(refreshToken);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var live = await context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null && token.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var token in live)
        {
            token.RevokedAtUtc = now;
        }
    }
}
