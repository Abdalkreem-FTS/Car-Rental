using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAtUtc) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    string GenerateRefreshToken();

    TimeSpan RefreshTokenLifetime { get; }
}
