namespace CarRental.Application.Contracts.Auth;

public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string Password,
    string ConfirmPassword);
