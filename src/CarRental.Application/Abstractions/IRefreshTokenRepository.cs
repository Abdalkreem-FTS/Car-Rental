using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    void Add(RefreshToken refreshToken);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
