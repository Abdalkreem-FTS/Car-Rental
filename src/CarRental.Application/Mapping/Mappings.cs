using CarRental.Application.Contracts.Auth;
using CarRental.Application.Contracts.Cars;
using CarRental.Application.Contracts.Profile;
using CarRental.Application.Contracts.Reservations;
using CarRental.Domain.Entities;

namespace CarRental.Application.Mapping;

public static class Mappings
{
    public static CarResponse ToResponse(this Car car) => new(
        car.Id,
        car.Make,
        car.Model,
        car.Year,
        car.PlateNumber,
        car.Location,
        car.DailyRate,
        car.Seats,
        car.Category,
        car.Transmission,
        car.Fuel,
        car.ImageUrl,
        car.Description,
        car.IsActive);

    extension(ApplicationUser user)
    {
        public UserResponse ToResponse(IReadOnlyList<string> roles) => new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            roles);

        public ProfileResponse ToProfileResponse(IReadOnlyList<string> roles) => new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            user.DateOfBirth,
            user.AddressLine1,
            user.AddressLine2,
            user.City,
            user.Country,
            user.DriverLicenseNumber,
            user.CreatedAtUtc,
            roles);
    }

    public static ReservationResponse ToResponse(this Reservation reservation)
    {
        var car = reservation.Car;

        return new ReservationResponse(
            reservation.Id,
            reservation.CarId,
            car?.Make ?? string.Empty,
            car?.Model ?? string.Empty,
            car?.Year ?? 0,
            car?.ImageUrl,
            reservation.StartDate,
            reservation.EndDate,
            reservation.TotalDays,
            car?.DailyRate ?? 0m,
            reservation.TotalPrice,
            reservation.Status,
            reservation.PickupLocation,
            reservation.CreatedAtUtc);
    }
}
