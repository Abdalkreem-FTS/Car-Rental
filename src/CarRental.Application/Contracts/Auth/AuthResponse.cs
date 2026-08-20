namespace CarRental.Application.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    UserResponse User);