using CarRental.Api.Contracts.Auth;
using CarRental.Api.Contracts.Cars;
using CarRental.Api.Contracts.Profile;
using CarRental.Api.Contracts.Reservations;
using CarRental.Domain.Enums;

namespace CarRental.IntegrationTests.Support;

public static class TestData
{
    public const string AdminEmail = "admin@carrental.test";
    public const string AdminPassword = "Admin#12345";

    public const string Password = "Str0ng#Pass1";
    public const string NewPassword = "N3w#Password9";

    public const int FleetSize = 16;

    public const int MaxFailedSignIns = 5;

    public static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    public static DateOnly In(int days) => Today.AddDays(days);

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public static string UniqueLicense() => $"JO-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    public static string UniquePlate() => $"TST-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    public static RegisterRequest Registration() => new(
        FirstName: "Lina",
        LastName: "Haddad",
        Email: UniqueEmail(),
        Password: Password,
        ConfirmPassword: Password,
        PhoneNumber: "+962791234567",
        DateOfBirth: new DateOnly(1996, 4, 12),
        AddressLine1: "22 Wasfi Al-Tal St",
        AddressLine2: "Apt 5",
        City: "Amman",
        Country: "Jordan",
        DriverLicenseNumber: UniqueLicense());

    public static UpdateProfileRequest ProfileUpdate() => new(
        FirstName: "Lina",
        LastName: "Haddad",
        PhoneNumber: "+962791234567",
        DateOfBirth: new DateOnly(1996, 4, 12),
        AddressLine1: "22 Wasfi Al-Tal St",
        AddressLine2: "Apt 5",
        City: "Amman",
        Country: "Jordan",
        DriverLicenseNumber: UniqueLicense());

    public static CreateCarRequest NewCar() => new(
        Make: "Mazda",
        Model: "CX-5",
        Year: 2024,
        PlateNumber: UniquePlate(),
        Location: "Amman",
        DailyRate: 62m,
        Seats: 5,
        Category: CarCategory.SUV,
        Transmission: TransmissionType.Automatic,
        Fuel: FuelType.Petrol,
        ImageUrl: null,
        Description: "Added by a test.");

    public static UpdateCarRequest CarUpdate(CreateCarRequest from) => new(
        from.Make,
        from.Model,
        from.Year,
        from.PlateNumber,
        from.Location,
        from.DailyRate,
        from.Seats,
        from.Category,
        from.Transmission,
        from.Fuel,
        from.ImageUrl,
        from.Description);

    public static CreateReservationRequest Booking(Guid carId, int fromDay, int toDay) =>
        new(carId, In(fromDay), In(toDay), PickupLocation: null);

    public static UpdateReservationRequest BookingChange(int fromDay, int toDay) =>
        new(In(fromDay), In(toDay), PickupLocation: null);
}
