using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        context.RefreshTokens
            .AsNoTracking()
            .Include(refreshToken => refreshToken.User)
            .FirstOrDefaultAsync(refreshToken => refreshToken.Token == token, cancellationToken);

    public async Task<bool> TrySpendAsync(string token, Guid replacedByTokenId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var spent = await context.RefreshTokens
            .Where(refreshToken =>
                refreshToken.Token == token &&
                refreshToken.RevokedAtUtc == null &&
                refreshToken.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(refreshToken => refreshToken.RevokedAtUtc, now)
                    .SetProperty(refreshToken => refreshToken.ReplacedByTokenId, replacedByTokenId),
                cancellationToken);

        return spent == 1;
    }

    public Task RevokeAsync(string token, Guid userId, CancellationToken cancellationToken = default) =>
        context.RefreshTokens
            .Where(refreshToken =>
                refreshToken.Token == token &&
                refreshToken.UserId == userId &&
                refreshToken.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(refreshToken => refreshToken.RevokedAtUtc, DateTimeOffset.UtcNow),
                cancellationToken);

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
