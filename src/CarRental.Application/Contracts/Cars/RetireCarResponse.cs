namespace CarRental.Application.Contracts.Cars;

public sealed record RetireCarResponse(CarResponse Car, int CancelledReservations);
