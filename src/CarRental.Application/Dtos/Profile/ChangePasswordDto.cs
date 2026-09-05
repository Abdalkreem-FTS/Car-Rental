namespace CarRental.Application.Dtos.Profile;

public sealed record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);
