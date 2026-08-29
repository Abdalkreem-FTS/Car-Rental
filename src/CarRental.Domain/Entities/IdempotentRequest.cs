namespace CarRental.Domain.Entities;

public sealed class IdempotentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid UserId { get; set; }

    public required string Endpoint { get; set; }

    public required string Key { get; set; }

    public required string RequestHash { get; set; }

    public int? StatusCode { get; set; }

    public string? ResponseBody { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }
}
