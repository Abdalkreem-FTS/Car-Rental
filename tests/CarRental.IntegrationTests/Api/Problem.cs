using System.Text.Json.Serialization;

namespace CarRental.IntegrationTests.Api;

public sealed record Problem
{
    public string? Title { get; init; }

    public int Status { get; init; }

    public string? Detail { get; init; }

    public Dictionary<string, string[]>? Errors { get; init; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }
}
