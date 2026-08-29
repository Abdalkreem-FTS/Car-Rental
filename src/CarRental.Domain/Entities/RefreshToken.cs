using System.Security.Cryptography;
using System.Text;

namespace CarRental.Domain.Entities;

public sealed class RefreshToken
{
    public static string HashOf(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid UserId { get; set; }

    public required string TokenHash { get; set; }

    public required DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public ApplicationUser? User { get; set; }

    public bool IsActive(DateTimeOffset utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public bool WasSpent => RevokedAtUtc is not null && ReplacedByTokenId is not null;
}
