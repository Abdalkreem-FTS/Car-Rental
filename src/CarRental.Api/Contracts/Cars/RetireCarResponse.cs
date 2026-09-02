namespace CarRental.Api.Contracts.Cars;

public sealed record RetireCarResponse(CarResponse Car, int CancelledReservations);
