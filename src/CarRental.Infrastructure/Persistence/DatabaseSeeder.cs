using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarRental.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<SeedOptions> options,
    ILogger<DatabaseSeeder> logger)
{
    private readonly SeedOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedAdminAsync();
        await SeedCarsAsync(cancellationToken);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            MustSucceed(await roleManager.CreateAsync(new ApplicationRole(role)), $"create the role {role}");
            logger.LogInformation("Seeded role {Role}", role);
        }
    }

    private async Task SeedAdminAsync()
    {
        if (await userManager.FindByEmailAsync(_options.AdminEmail) is not null)
        {
            return;
        }

        var admin = ApplicationUser.RegisterAdministrator(
            _options.AdminEmail,
            "Site",
            "Administrator",
            "+962790000000");

        admin.EmailConfirmed = true;

        MustSucceed(await userManager.CreateAsync(admin, _options.AdminPassword), "create the administrator");
        MustSucceed(await userManager.AddToRolesAsync(admin, [Roles.Admin, Roles.Customer]), "give the administrator its roles");

        logger.LogInformation("Seeded admin account {Email}", _options.AdminEmail);
    }

    private static void MustSucceed(IdentityResult result, string attempt)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seeding could not {attempt}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }

    private async Task SeedCarsAsync(CancellationToken cancellationToken)
    {
        if (await context.Cars.AnyAsync(cancellationToken))
        {
            return;
        }

        context.Cars.AddRange(DemoFleet());
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded the demo fleet");
    }

    private static IEnumerable<Car> DemoFleet() =>
    [
        NewCar("Toyota", "Corolla", 2023, "AMM-1201", "Amman", 32m, 5, CarCategory.Compact, TransmissionType.Automatic, FuelType.Petrol,
            "Reliable and light on fuel — the easiest way to get around the city."),
        NewCar("Hyundai", "Elantra", 2022, "AMM-1202", "Amman", 35m, 5, CarCategory.Sedan, TransmissionType.Automatic, FuelType.Petrol,
            "Roomy sedan with a generous boot, well suited to airport runs."),
        NewCar("Kia", "Picanto", 2023, "AMM-1203", "Amman", 24m, 4, CarCategory.Economy, TransmissionType.Manual, FuelType.Petrol,
            "Our cheapest way to get on the road. Parks anywhere."),
        NewCar("Tesla", "Model 3", 2024, "AMM-1204", "Amman", 89m, 5, CarCategory.Luxury, TransmissionType.Automatic, FuelType.Electric,
            "Long-range electric with autopilot and a 15-inch centre display."),
        NewCar("Nissan", "Kicks", 2023, "AMM-1205", "Amman", 44m, 5, CarCategory.SUV, TransmissionType.Automatic, FuelType.Petrol,
            "Compact crossover with a high seating position and a big boot."),
        NewCar("Mercedes-Benz", "E 200", 2023, "AMM-1206", "Amman", 145m, 5, CarCategory.Luxury, TransmissionType.Automatic, FuelType.Hybrid,
            "Executive saloon with leather trim and adaptive cruise control."),
        NewCar("Volkswagen", "Golf", 2022, "IRB-2101", "Irbid", 38m, 5, CarCategory.Compact, TransmissionType.Manual, FuelType.Diesel,
            "Sharp handling and excellent motorway economy."),
        NewCar("Toyota", "RAV4", 2023, "IRB-2102", "Irbid", 58m, 5, CarCategory.SUV, TransmissionType.Automatic, FuelType.Hybrid,
            "Hybrid SUV that is happy on rough roads and quiet in traffic."),
        NewCar("Skoda", "Octavia", 2022, "IRB-2103", "Irbid", 41m, 5, CarCategory.Sedan, TransmissionType.Automatic, FuelType.Diesel,
            "Enormous boot for the money — a favourite with families."),
        NewCar("Ford", "Ranger", 2023, "AQB-3301", "Aqaba", 72m, 5, CarCategory.Pickup, TransmissionType.Automatic, FuelType.Diesel,
            "Double-cab pickup with four-wheel drive for desert tracks."),
        NewCar("Jeep", "Wrangler", 2023, "AQB-3302", "Aqaba", 95m, 4, CarCategory.SUV, TransmissionType.Automatic, FuelType.Petrol,
            "Removable roof and proper off-road gearing for Wadi Rum."),
        NewCar("Hyundai", "Accent", 2022, "AQB-3303", "Aqaba", 27m, 5, CarCategory.Economy, TransmissionType.Automatic, FuelType.Petrol,
            "Small, cold air conditioning, ideal for the coast road."),
        NewCar("Kia", "Carnival", 2023, "ZRQ-4401", "Zarqa", 78m, 8, CarCategory.Van, TransmissionType.Automatic, FuelType.Diesel,
            "Eight seats and sliding doors — the group and luggage option."),
        NewCar("Toyota", "Hiace", 2021, "ZRQ-4402", "Zarqa", 85m, 12, CarCategory.Van, TransmissionType.Manual, FuelType.Diesel,
            "Twelve-seat minibus for tour groups and large families."),
        NewCar("BMW", "X5", 2024, "DSA-5501", "Dead Sea", 165m, 5, CarCategory.Luxury, TransmissionType.Automatic, FuelType.Hybrid,
            "Full-size luxury SUV with panoramic roof and heated seats."),
        NewCar("Renault", "Clio", 2022, "DSA-5502", "Dead Sea", 29m, 5, CarCategory.Economy, TransmissionType.Manual, FuelType.Petrol,
            "Nimble hatchback that sips fuel on the resort road."),
    ];

    private static Car NewCar(
        string make,
        string model,
        int year,
        string plate,
        string location,
        decimal dailyRate,
        int seats,
        CarCategory category,
        TransmissionType transmission,
        FuelType fuel,
        string description) => new()
        {
            Make = make,
            Model = model,
            Year = year,
            PlateNumber = plate,
            Location = location,
            DailyRate = dailyRate,
            Seats = seats,
            Category = category,
            Transmission = transmission,
            Fuel = fuel,
            Description = description,
        };
}
