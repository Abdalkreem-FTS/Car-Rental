using CarRental.Domain.Enums;

namespace CarRental.Domain.Entities;

public sealed class OutboxEmail
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required OutboxEmailKind Kind { get; set; }

    public required string Recipient { get; set; }

    public required string FirstName { get; set; }

    public required string Link { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset NextAttemptAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SentAtUtc { get; set; }

    public DateTimeOffset? AbandonedAtUtc { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
