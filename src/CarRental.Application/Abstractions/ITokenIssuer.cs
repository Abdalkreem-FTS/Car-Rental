using CarRental.Application.Contracts.Auth;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface ITokenIssuer
{
    Task<Result<AuthResponse>> IssueAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RotateAsync(string presentedToken, CancellationToken cancellationToken = default);

    Task<Result<Success>> RevokeAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken = default);

    Task<Result<Success>> RevokeEverythingAsync(Guid userId, CancellationToken cancellationToken = default);
}
