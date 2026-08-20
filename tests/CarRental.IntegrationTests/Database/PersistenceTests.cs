using CarRental.Domain.Entities;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using static CarRental.IntegrationTests.Infrastructure.TestData;

namespace CarRental.IntegrationTests.Database;

public sealed class PersistenceTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_WhenAnAccountIsCreated_StoresThePasswordAsAVersionedPbkdf2Hash()
    {
        var auth = await SignUpAsync();

        var hash = (await StoredUserAsync(auth.User.Email)).PasswordHash.ShouldNotBeNull();

        hash.ShouldNotContain(Password);

        var raw = Convert.FromBase64String(hash);

        raw[0].ShouldBe((byte)1, "the hash must be in the current versioned format");
        BitConverter.ToUInt32([.. raw[1..5].Reverse()]).ShouldBe(2u, "HMACSHA512");
        BitConverter.ToUInt32([.. raw[5..9].Reverse()]).ShouldBe(100_000u);

        var saltLength = BitConverter.ToUInt32([.. raw[9..13].Reverse()]);
        saltLength.ShouldBe(16u);
        (raw.Length - 13 - saltLength).ShouldBe(32u, "a 256-bit subkey");
    }

    [Fact]
    public async Task Register_WithTheSamePasswordAsAnotherAccount_StoresADifferentHash()
    {
        var first = await SignUpAsync();
        var second = await SignUpAsync();

        var firstHash = (await StoredUserAsync(first.User.Email)).PasswordHash;
        var secondHash = (await StoredUserAsync(second.User.Email)).PasswordHash;

        firstHash.ShouldNotBe(secondHash);
    }

    [Fact]
    public async Task Register_WithAMixedCaseEmail_KeepsTheCasingAndIndexesTheNormalisedForm()
    {
        var email = $"MiXeD.{Guid.NewGuid():N}@Example.COM";

        await SignUpAsync(Registration() with { Email = email });

        var user = await StoredUserAsync(email);

        user.Email.ShouldBe(email, "the address is shown back to the user as they wrote it");
        user.NormalizedEmail.ShouldBe(email.ToUpperInvariant());
    }

    [Fact]
    public async Task SaveChanges_WithADuplicateNormalisedEmail_RaisesAUniqueViolation()
    {
        var auth = await SignUpAsync();
        var existing = await StoredUserAsync(auth.User.Email);

        await ShouldViolateAsync(Insert, PostgresErrorCodes.UniqueViolation);
        return;

        Task<int> Insert() =>
            Factory.WithDbAsync(async db =>
            {
                db.Users.Add(new ApplicationUser
                {
                    UserName = UniqueEmail(),
                    NormalizedUserName = UniqueEmail().ToUpperInvariant(),
                    Email = existing.Email!.ToUpperInvariant(),
                    NormalizedEmail = existing.NormalizedEmail,
                    PasswordHash = "irrelevant",
                    PhoneNumber = "+962790000000",
                    FirstName = "Copy",
                    LastName = "Cat",
                    AddressLine1 = "1 St",
                    City = "Amman",
                    Country = "Jordan",
                    DriverLicenseNumber = UniqueLicense(),
                });

                return await db.SaveChangesAsync();
            });
    }

    [Fact]
    public async Task SaveChanges_WithADuplicatePlateNumber_RaisesAUniqueViolation()
    {
        await ShouldViolateAsync(Insert, PostgresErrorCodes.UniqueViolation);
        return;

        Task<int> Insert() =>
            Factory.WithDbAsync(async db =>
            {
                var existing = await db.Cars.AsNoTracking().FirstAsync();

                db.Cars.Add(new Car
                {
                    Make = "Clone",
                    Model = "Car",
                    Year = 2024,
                    PlateNumber = existing.PlateNumber,
                    Location = "Amman",
                    DailyRate = 40m,
                    Seats = 5,
                });

                return await db.SaveChangesAsync();
            });
    }

    [Fact]
    public async Task SaveChanges_WhenDeletingACarThatHasReservations_RaisesAForeignKeyViolation()
    {
        await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        await ShouldViolateAsync(Delete, PostgresErrorCodes.ForeignKeyViolation);
        return;

        Task<int> Delete() =>
            Factory.WithDbAsync(async db =>
            {
                db.Cars.Remove(await db.Cars.SingleAsync(c => c.Id == car.Id));

                return await db.SaveChangesAsync();
            });
    }

    [Fact]
    public async Task SaveChanges_WhenDeletingAUser_CascadesToTheirTokensRolesAndReservations()
    {
        var auth = await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        await Factory.WithDbAsync(async db =>
        {
            db.Users.Remove(await db.Users.SingleAsync(u => u.Id == auth.User.Id));

            return await db.SaveChangesAsync();
        });

        var leftovers = await Factory.WithDbAsync(async db => new
        {
            RefreshTokens = await db.RefreshTokens.CountAsync(t => t.UserId == auth.User.Id),
            Roles = await db.UserRoles.CountAsync(r => r.UserId == auth.User.Id),
            Reservations = await db.Reservations.CountAsync(r => r.UserId == auth.User.Id),
            Cars = await db.Cars.CountAsync(),
        });

        leftovers.RefreshTokens.ShouldBe(0);
        leftovers.Roles.ShouldBe(0);
        leftovers.Reservations.ShouldBe(0);
        leftovers.Cars.ShouldBe(FleetSize, "the fleet is not owned by any one customer");
    }

    [Fact]
    public async Task Refresh_WhenATokenIsRotated_KeepsTheOldRowAndStampsItRevoked()
    {
        var auth = await SignUpAsync();

        (await Api.Auth.RefreshAsync(auth.RefreshToken)).ShouldBeOk();

        var tokens = await Factory.WithDbAsync(db => db.RefreshTokens
            .Where(t => t.UserId == auth.User.Id)
            .OrderBy(t => t.CreatedAtUtc)
            .ToListAsync());

        tokens.Count.ShouldBe(2);
        tokens[0].Token.ShouldBe(auth.RefreshToken);
        tokens[0].RevokedAtUtc.ShouldNotBeNull();
        tokens[1].RevokedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task CreateReservation_WhenTheTotalIsSaved_StoresItAtExactDecimalScale()
    {
        await SignUpAsync();
        var car = await FindCarAsync("E 200");

        (await Api.Reservations.CreateAsync(Booking(car.Id, 3, 5))).ShouldBeCreated();

        var stored = await Factory.Database.ScalarAsync("SELECT \"TotalPrice\"::text FROM \"Reservations\" LIMIT 1");

        stored.ShouldBe("435.00");
    }

    [Fact]
    public async Task CreateReservation_WhenRentalDaysAreSaved_RoundTripsThemWithoutAShift()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        (await Api.Reservations.CreateAsync(Booking(car.Id, 10, 12))).ShouldBeCreated();

        var storedStart = await Factory.Database.ScalarAsync("SELECT \"StartDate\"::text FROM \"Reservations\" LIMIT 1");
        var storedEnd = await Factory.Database.ScalarAsync("SELECT \"EndDate\"::text FROM \"Reservations\" LIMIT 1");

        storedStart.ShouldBe(In(10).ToString("yyyy-MM-dd"));
        storedEnd.ShouldBe(In(12).ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task SaveChanges_WhenARoleIsAssigned_StoresItByName()
    {
        await SignUpAsync();

        var roles = await Factory.WithDbAsync(db => db.Roles.Select(r => r.Name).ToListAsync());

        roles.ShouldContain(Roles.Customer);
        roles.ShouldContain(Roles.Admin);
    }

    [Fact]
    public async Task Seed_WhenTheAdminIsCreated_GivesItBothRolesAndAHashedPassword()
    {
        var admin = await StoredUserAsync(AdminEmail);

        var roleNames = await Factory.WithDbAsync(db =>
            (from userRole in db.UserRoles
             join role in db.Roles on userRole.RoleId equals role.Id
             where userRole.UserId == admin.Id
             orderby role.Name
             select role.Name!).ToListAsync());

        roleNames.ShouldBe([Roles.Admin, Roles.Customer]);
        admin.PasswordHash.ShouldNotBeNullOrWhiteSpace();
        admin.PasswordHash.ShouldNotBe(AdminPassword);
    }

    [Fact]
    public async Task Seed_WhenRunTwice_ChangesNothing()
    {
        var before = await CountsAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider
            .GetRequiredService<CarRental.Infrastructure.Persistence.DatabaseSeeder>()
            .SeedAsync();

        (await CountsAsync()).ShouldBeEquivalentTo(before);
    }

    private Task<object> CountsAsync() => Factory.WithDbAsync<object>(async db => new
    {
        Users = await db.Users.CountAsync(),
        Cars = await db.Cars.CountAsync(),
        Roles = await db.Roles.CountAsync(),
        UserRoles = await db.UserRoles.CountAsync(),
    });

    private static async Task ShouldViolateAsync(Func<Task<int>> write, string expectedSqlState)
    {
        var exception = await write.ShouldThrowAsync<DbUpdateException>();

        exception.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe(expectedSqlState);
    }
}
