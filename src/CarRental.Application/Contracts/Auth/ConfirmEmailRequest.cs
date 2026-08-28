namespace CarRental.Application.Contracts.Auth;

public sealed record ConfirmEmailRequest(string Email, string Token);
