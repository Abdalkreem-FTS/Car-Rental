namespace CarRental.Application.Dtos.Cars;

public sealed record RetireCarDto(bool CancelActiveBookings = false);
