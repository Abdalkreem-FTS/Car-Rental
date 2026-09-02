using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Database;

public sealed class OverlapConstraintTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Database_WhenTwoConfirmedBookingsOverlap_RefusesTheSecond()
    {
        var (userId, carId) = await ABookableCarAsync();

        await InsertAsync(userId, carId, In(40), In(45), ReservationStatus.Confirmed);

        var second = await Should.ThrowAsync<DbUpdateException>(
            () => InsertAsync(userId, carId, In(43), In(47), ReservationStatus.Confirmed));

        Postgres(second).ShouldNotBeNull().ConstraintName
            .ShouldBe("ck_reservations_no_overlapping_confirmed_bookings");
    }

    [Fact]
    public async Task Database_WhenTheOverlapIsCancelled_AllowsIt()
    {
        var (userId, carId) = await ABookableCarAsync();

        await InsertAsync(userId, carId, In(60), In(65), ReservationStatus.Cancelled);

        await InsertAsync(userId, carId, In(62), In(67), ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task Database_ForBookingsOnDifferentCars_AllowsTheSameDays()
    {
        var (userId, first) = await ABookableCarAsync();
        var second = (await Api.Cars.SearchAsync(CarQuery.Matching("Wrangler"))).ShouldBeOk().Items[0].Id;

        await InsertAsync(userId, first, In(80), In(85), ReservationStatus.Confirmed);
        await InsertAsync(userId, second, In(80), In(85), ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task Status_IsStoredAsTheTextTheConstraintMatchesOn()
    {
        var (userId, carId) = await ABookableCarAsync();

        await InsertAsync(userId, carId, In(90), In(92), ReservationStatus.Confirmed);

        var stored = await Factory.Database.QuerySingleAsync<string>(
            """SELECT DISTINCT "Status" FROM "Reservations" WHERE "Status" = 'Confirmed' LIMIT 1""");

        stored.ShouldBe(
            nameof(ReservationStatus.Confirmed),
            "the partial constraint matches on this text; storing the enum as an ordinal would silently disable it");
    }

    [Fact]
    public async Task Database_ForBookingsThatOnlyTouchOnTheChangeoverDay_RefusesTheSecond()
    {
        var (userId, carId) = await ABookableCarAsync();

        await InsertAsync(userId, carId, In(100), In(105), ReservationStatus.Confirmed);

        var sameDayChangeover = await Should.ThrowAsync<DbUpdateException>(
            () => InsertAsync(userId, carId, In(105), In(110), ReservationStatus.Confirmed));

        Postgres(sameDayChangeover).ShouldNotBeNull().SqlState.ShouldBe(
            PostgresErrorCodes.ExclusionViolation,
            "the inclusive bound treats the return day as still occupied, so there is no same-day turnaround");
    }

    private async Task<(Guid UserId, Guid CarId)> ABookableCarAsync()
    {
        var auth = await SignUpAsync();
        var car = await FindCarAsync("Hiace");

        return (auth.User.Id, car.Id);
    }

    private Task InsertAsync(Guid userId, Guid carId, DateOnly start, DateOnly end, ReservationStatus status) =>
        Factory.WithDbAsync(async db =>
        {
            db.Reservations.Add(new Reservation
            {
                UserId = userId,
                CarId = carId,
                StartDate = start,
                EndDate = end,
                DailyRate = 50m,
                TotalPrice = 50m,
                Status = status,
            });

            return await db.SaveChangesAsync();
        });

    private static PostgresException? Postgres(Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            if (exception is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }
}
