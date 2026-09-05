using CarRental.Application.Dtos.Auth;
using CarRental.Domain.Common;

namespace CarRental.Application.Abstractions;

public interface ISessionService
{
    Task<Result<AuthDto>> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);

    Task<Result<AuthDto>> RefreshAsync(RefreshTokenDto request, CancellationToken cancellationToken = default);

    Task<Result<Success>> LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken = default);

    Task<Result<Success>> LogoutEverywhereAsync(Guid userId, CancellationToken cancellationToken = default);
}
