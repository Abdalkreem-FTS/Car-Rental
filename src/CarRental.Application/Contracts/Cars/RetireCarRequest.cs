namespace CarRental.Application.Contracts.Cars;

public sealed record RetireCarRequest(bool CancelActiveBookings = false);
