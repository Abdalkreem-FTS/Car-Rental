using CarRental.Application.Contracts.Auth;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface ISessionService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task<Result<Success>> LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken = default);

    Task<Result<Success>> LogoutEverywhereAsync(Guid userId, CancellationToken cancellationToken = default);
}
