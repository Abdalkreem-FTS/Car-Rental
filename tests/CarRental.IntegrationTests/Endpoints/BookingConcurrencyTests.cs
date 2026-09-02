using System.Net;
using CarRental.Api.Contracts.Reservations;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class BookingConcurrencyTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    private const int Racers = 8;

    [Fact]
    public async Task CreateReservation_WhenManyRequestsRaceForTheSameDays_LetsExactlyOneThrough()
    {
        var accessToken = (await SignUpAsync()).AccessToken;
        var car = await FindCarAsync("RAV4");

        var responses = await RaceAsync(accessToken, api => api.Reservations.CreateAsync(Booking(car.Id, 40, 45)));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);

        foreach (var loser in responses.Where(r => r.StatusCode != HttpStatusCode.Created))
        {
            loser.ShouldBeConflict(CarErrors.Unavailable);
        }

        var stored = await Factory.WithDbAsync(db => db.Reservations.CountAsync(r => r.CarId == car.Id));
        stored.ShouldBe(1, "the database must hold one booking, whatever the API replied");
    }

    [Fact]
    public async Task CreateReservation_WhenRacingRequestsOnlyAbut_AllSucceed()
    {
        var accessToken = (await SignUpAsync()).AccessToken;
        var car = await FindCarAsync("Wrangler");

        var windows = Enumerable.Range(0, Racers).Select(i => (From: 60 + (i * 3), To: 62 + (i * 3))).ToList();

        var responses = await Task.WhenAll(windows.Select(async window =>
        {
            using var api = AuthenticatedClient(accessToken);

            return await api.Reservations.CreateAsync(Booking(car.Id, window.From, window.To));
        }));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created);
        (await Factory.WithDbAsync(db => db.Reservations.CountAsync(r => r.CarId == car.Id))).ShouldBe(Racers);
    }

    [Fact]
    public async Task UpdateReservation_WhenManyMovesRaceForTheSameDays_LetsExactlyOneThrough()
    {
        var accessToken = (await SignUpAsync()).AccessToken;
        var car = await FindCarAsync("Corolla");

        var reservations = new List<ReservationResponse>();

        for (var i = 0; i < Racers; i++)
        {
            reservations.Add((await Api.Reservations.CreateAsync(Booking(car.Id, 100 + (i * 3), 101 + (i * 3))))
                .ShouldBeCreated());
        }

        var responses = await Task.WhenAll(reservations.Select(async reservation =>
        {
            using var api = AuthenticatedClient(accessToken);

            return await api.Reservations.UpdateAsync(reservation, BookingChange(200, 205));
        }));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);

        var onTheContestedWindow = await Factory.WithDbAsync(db =>
            db.Reservations.CountAsync(r => r.CarId == car.Id && r.StartDate == In(200)));

        onTheContestedWindow.ShouldBe(1);
    }

    [Fact]
    public async Task CreateReservation_ForDaysACancelledBookingHeld_Succeeds()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Picanto");

        var reservation = (await Api.Reservations.CreateAsync(Booking(car.Id, 40, 45))).ShouldBeCreated();
        (await Api.Reservations.CancelAsync(reservation.Id)).ShouldBeNoContent();

        (await Api.Reservations.CreateAsync(Booking(car.Id, 40, 45))).ShouldBeCreated();
    }

    private async Task<IReadOnlyList<ApiResponse<T>>> RaceAsync<T>(
        string accessToken,
        Func<CarRentalApi, Task<ApiResponse<T>>> call)
        where T : class
    {
        var clients = Enumerable.Range(0, Racers).Select(_ => AuthenticatedClient(accessToken)).ToList();

        try
        {
            return await Task.WhenAll(clients.Select(call));
        }
        finally
        {
            clients.ForEach(client => client.Dispose());
        }
    }

    private CarRentalApi AuthenticatedClient(string accessToken)
    {
        var api = new CarRentalApi(Factory.CreateClient());
        api.Authenticate(accessToken);

        return api;
    }
}
