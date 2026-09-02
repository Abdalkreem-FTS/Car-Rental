namespace CarRental.Application.Dtos.Auth;

public sealed record AuthDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAtUtc,
    DateTimeOffset ExpiresAtUtc,
    UserDto User);
