namespace CarRental.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid UserId { get; set; }

    public required string Token { get; set; }

    public required DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public ApplicationUser? User { get; set; }

    public bool IsActive(DateTimeOffset utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;
}
