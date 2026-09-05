using CarRental.Application.Abstractions;
using CarRental.Application.Dtos.Auth;
using CarRental.Application.Mapping;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CarRental.Application.Services;

public sealed class TokenIssuer(
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    ILogger<TokenIssuer> logger) : ITokenIssuer
{
    public Task<Result<AuthDto>> IssueAsync(ApplicationUser user, CancellationToken cancellationToken = default) =>
        IssueAsync(user, NewRefreshToken(user.Id), cancellationToken);

    public async Task<Result<AuthDto>> RotateAsync(string presentedToken, CancellationToken cancellationToken = default)
    {
        var stored = await refreshTokens.GetByTokenAsync(presentedToken, cancellationToken);

        if (stored is null || await userManager.FindByIdAsync(stored.UserId.ToString()) is not { } user)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        var replacement = NewRefreshToken(stored.UserId);

        if (await refreshTokens.TrySpendAsync(presentedToken, replacement.Entity.Id, cancellationToken))
        {
            return await IssueAsync(user, replacement, cancellationToken);
        }

        return await RefusalFor(presentedToken, stored.UserId, cancellationToken);
    }

    public async Task<Result<Success>> RevokeAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Success;
        }

        await refreshTokens.RevokeAsync(refreshToken, userId, cancellationToken);

        return Result.Success;
    }

    public async Task<Result<Success>> RevokeEverythingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await refreshTokens.RevokeAllForUserAsync(userId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    private async Task<Result<AuthDto>> RefusalFor(string token, Guid userId, CancellationToken cancellationToken)
    {
        var current = await refreshTokens.GetByTokenAsync(token, cancellationToken);

        if (current is not { WasSpent: true }
            || DateTimeOffset.UtcNow - current.RevokedAtUtc!.Value <= tokenGenerator.RefreshReuseLeeway)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        logger.LogWarning("Refresh token replayed for {UserId}. Revoking every session.", userId);

        await refreshTokens.RevokeAllForUserAsync(userId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthErrors.RefreshTokenReused;
    }

    private (RefreshToken Entity, string Token) NewRefreshToken(Guid userId)
    {
        var token = tokenGenerator.GenerateRefreshToken();

        return (new RefreshToken
        {
            UserId = userId,
            TokenHash = RefreshToken.HashOf(token),
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(tokenGenerator.RefreshTokenLifetime),
        }, token);
    }

    private async Task<Result<AuthDto>> IssueAsync(
        ApplicationUser user,
        (RefreshToken Entity, string Token) refreshToken,
        CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (accessToken, expiresAtUtc) = tokenGenerator.GenerateAccessToken(user, roles);

        refreshTokens.Add(refreshToken.Entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthDto(
            accessToken,
            refreshToken.Token,
            refreshToken.Entity.ExpiresAtUtc,
            expiresAtUtc,
            user.ToResponse(roles));
    }
}
