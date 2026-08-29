using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Shouldly;
using static CarRental.IntegrationTests.Infrastructure.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class ReservationConcurrencyControlTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateReservation_WhenBooked_CarriesAnETag()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        var response = await Api.Http.SendAsync(Post(Routes.Reservations.Base, Booking(car.Id, 5, 7)));

        response.Headers.ETag.ShouldNotBeNull();
        response.Headers.ETag!.Tag.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UpdateReservation_WithNoIfMatch_IsRefused()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        var response = await Api.Reservations.UpdateWithoutVersionAsync(reservation.Id, BookingChange(20, 22));

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.PreconditionRequired);
        response.Problem!.ErrorCode.ShouldBe(ReservationErrors.VersionRequired.Code);
    }

    [Fact]
    public async Task UpdateReservation_WithTheVersionItWasRead_Succeeds()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        var moved = (await Api.Reservations.UpdateAsync(reservation, BookingChange(20, 22))).ShouldBeOk();

        moved.Version.ShouldNotBe(reservation.Version, "a change must produce a new version");
    }

    [Fact]
    public async Task UpdateReservation_FromASecondDeviceHoldingAStaleVersion_IsRefused()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        // Both devices read the same booking.
        var onPhone = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();
        var onLaptop = onPhone;

        (await Api.Reservations.UpdateAsync(onPhone, BookingChange(20, 22))).ShouldBeOk();

        var response = await Api.Reservations.UpdateAsync(onLaptop, BookingChange(30, 32));

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.PreconditionFailed);
        response.Problem!.ErrorCode.ShouldBe(ReservationErrors.VersionStale.Code);
    }

    [Fact]
    public async Task UpdateReservation_AfterReReadingTheBooking_SucceedsAgain()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        (await Api.Reservations.UpdateAsync(reservation, BookingChange(20, 22))).ShouldBeOk();

        var reread = (await Api.Reservations.GetAsync(reservation.Id)).ShouldBeOk();

        (await Api.Reservations.UpdateAsync(reread, BookingChange(30, 32))).ShouldBeOk();
    }

    [Fact]
    public async Task GetReservation_WhenRead_CarriesTheSameVersionAsTheBody()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        var response = await Api.Http.GetAsync(Routes.Reservations.ById(reservation.Id));
        var body = (await Api.Reservations.GetAsync(reservation.Id)).ShouldBeOk();

        response.Headers.ETag!.Tag.Trim('"').ShouldBe(body.Version);
    }

    private static HttpRequestMessage Post(string route, object body) =>
        new(HttpMethod.Post, route)
        {
            Content = System.Net.Http.Json.JsonContent.Create(body, options: CarRentalApi.Json),
            Headers = { { "Idempotency-Key", Guid.NewGuid().ToString("N") } },
        };
}
