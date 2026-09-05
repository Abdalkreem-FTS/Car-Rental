using System.Text.Json.Serialization;

namespace CarRental.Api.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    [property: JsonIgnore] string RefreshToken,
    [property: JsonIgnore] DateTimeOffset RefreshExpiresAtUtc,
    DateTimeOffset ExpiresAtUtc,
    UserResponse User);
