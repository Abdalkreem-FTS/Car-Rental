using CarRental.Application.Contracts.Cars;

namespace CarRental.Application.Validators.Car;

public sealed class CreateCarRequestValidator : CarDetailsValidator<CreateCarRequest>;
