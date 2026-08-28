using System.Text.Json;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static CarRental.IntegrationTests.Infrastructure.TestData;

namespace CarRental.IntegrationTests.Database;

public sealed class TimestampTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Read_ForAnyPersistedTimestamp_ReturnsItAtOffsetZero()
    {
        await SignUpAsync();

        var offsets = await Factory.WithDbAsync(async db => new List<TimeSpan>
        {
            (await db.Users.FirstAsync()).CreatedAtUtc.Offset,
            (await db.RefreshTokens.FirstAsync()).CreatedAtUtc.Offset,
            (await db.RefreshTokens.FirstAsync()).ExpiresAtUtc.Offset,
        });
        
        offsets.ShouldAllBe(offset => offset == TimeSpan.Zero);
    }

    [Fact]
    public async Task SaveChanges_WithANonZeroOffset_NormalisesItAndPreservesTheInstant()
    {
        var auth = await SignUpAsync();

        var instant = new DateTimeOffset(2026, 9, 1, 15, 0, 0, TimeSpan.FromHours(3));

        await Factory.WithDbAsync(async db =>
        {
            var token = await db.RefreshTokens.FirstAsync(t => t.UserId == auth.User.Id);
            token.ExpiresAtUtc = instant;

            return await db.SaveChangesAsync();
        });

        var stored = await Factory.WithDbAsync(db =>
            db.RefreshTokens.AsNoTracking().FirstAsync(t => t.UserId == auth.User.Id));
        
        stored.ExpiresAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        stored.ExpiresAtUtc.ShouldBe(instant);
        stored.ExpiresAtUtc.UtcDateTime.ShouldBe(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task IsLockedOut_AfterARoundTripThroughTheDatabase_ReachesTheSameVerdict()
    {
        var auth = await SignUpAsync();
        SignOut();
        await LockOutAsync(auth.User.Email);

        var user = await StoredUserAsync(auth.User.Email);

        user.LockoutEnd.ShouldNotBeNull();
        user.LockoutEnd.Value.Offset.ShouldBe(TimeSpan.Zero);
        user.LockoutEnd.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        user.LockoutEnd.Value.ShouldBeLessThan(DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Fact]
    public async Task Register_WhenSerialisingTheResponse_WritesTimestampsWithAnExplicitOffset()
    {
        var response = await Api.Auth.RegisterAsync(Registration());

        response.ShouldBeOk();

        using var document = JsonDocument.Parse(response.RawBody);
        var expiresAt = document.RootElement.GetProperty("expiresAtUtc").GetString();
        
        expiresAt.ShouldNotBeNull();
        expiresAt.ShouldEndWith("+00:00");
        DateTimeOffset.Parse(expiresAt).Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task CreateReservation_WhenSerialisingTheResponse_LeavesRentalDaysAsPlainDates()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        var response = await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7));

        response.ShouldBeCreated();

        using var document = JsonDocument.Parse(response.RawBody);
        
        document.RootElement.GetProperty("startDate").GetString().ShouldBe(In(5).ToString("yyyy-MM-dd"));
        document.RootElement.GetProperty("createdAtUtc").GetString().ShouldEndWith("+00:00");
    }
}
