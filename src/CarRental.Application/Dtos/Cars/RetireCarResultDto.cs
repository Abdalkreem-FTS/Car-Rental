namespace CarRental.Application.Dtos.Cars;

public sealed record RetireCarResultDto(CarDto Car, int CancelledReservations);
