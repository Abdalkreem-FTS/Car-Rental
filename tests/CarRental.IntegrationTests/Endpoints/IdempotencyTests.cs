using System.Net;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class IdempotencyTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateReservation_WithNoKey_IsRefused()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        var response = await Api.Reservations.CreateWithoutIdempotencyKeyAsync(Booking(car.Id, 5, 7));

        response.ShouldFailValidationOn("Idempotency-Key");
    }

    [Fact]
    public async Task CreateReservation_SentTwiceWithOneKey_BooksOnce()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        var key = Guid.NewGuid().ToString("N");
        var booking = Booking(car.Id, 5, 7);

        var first = (await Api.Reservations.CreateAsync(booking, key)).ShouldBeCreated();
        var second = (await Api.Reservations.CreateAsync(booking, key)).ShouldBeCreated();

        second.Id.ShouldBe(first.Id, "the replay must return the booking that was already made");

        var stored = await Factory.WithDbAsync(db => db.Reservations.CountAsync(r => r.CarId == car.Id));
        stored.ShouldBe(1);
    }

    [Fact]
    public async Task CreateReservation_WhenReplayed_SaysSo()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Picanto");
        var key = Guid.NewGuid().ToString("N");
        var booking = Booking(car.Id, 5, 7);

        (await Api.Reservations.CreateAsync(booking, key)).ShouldBeCreated();

        using var request = new HttpRequestMessage(HttpMethod.Post, Routes.Reservations.Base)
        {
            Content = System.Net.Http.Json.JsonContent.Create(booking, options: CarRentalApi.Json),
        };
        request.Headers.Add("Idempotency-Key", key);

        var replay = await Api.Http.SendAsync(request);

        replay.Headers.GetValues("Idempotent-Replay").ShouldBe(["true"]);
    }

    [Fact]
    public async Task CreateReservation_ReusingAKeyForADifferentBooking_IsRefused()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Kicks");
        var key = Guid.NewGuid().ToString("N");

        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7), key)).ShouldBeCreated();

        var response = await Api.Reservations.CreateAsync(Booking(car.Id, 20, 22), key);

        response.ShouldBeConflict(IdempotencyErrors.KeyReused);
    }

    [Fact]
    public async Task CreateReservation_WhenTheFirstAttemptFailed_LetsTheSameKeyTryAgain()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Elantra");
        var key = Guid.NewGuid().ToString("N");

        // Booking a car that is already taken fails, so the key must not be spent on that failure.
        var blocker = (await Api.Reservations.CreateAsync(Booking(car.Id, 30, 34))).ShouldBeCreated();
        blocker.ShouldNotBeNull();

        (await Api.Reservations.CreateAsync(Booking(car.Id, 31, 33), key))
            .ShouldBeConflict(CarErrors.Unavailable);

        (await Api.Reservations.CancelAsync(blocker.Id)).ShouldBeNoContent();

        (await Api.Reservations.CreateAsync(Booking(car.Id, 31, 33), key)).ShouldBeCreated();
    }

    [Fact]
    public async Task CreateReservation_WithAKeyAnotherAccountUsed_IsUnaffected()
    {
        var key = Guid.NewGuid().ToString("N");

        await SignUpAsync();
        var car = await FindCarAsync("Accent");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7), key)).ShouldBeCreated();

        await SignUpAsync();

        (await Api.Reservations.CreateAsync(Booking(car.Id, 40, 42), key)).ShouldBeCreated();
    }

    [Fact]
    public async Task CreateReservation_WhenManyRequestsShareOneKey_BooksOnce()
    {
        var auth = await SignUpAsync();
        var car = await FindCarAsync("RAV4");
        var key = Guid.NewGuid().ToString("N");
        var booking = Booking(car.Id, 50, 52);

        var clients = Enumerable.Range(0, 6).Select(_ =>
        {
            var api = new CarRentalApi(Factory.CreateClient());
            api.Authenticate(auth.AccessToken);

            return api;
        }).ToList();

        try
        {
            var responses = await Task.WhenAll(clients.Select(client => client.Reservations.CreateAsync(booking, key)));

            responses.Count(response => response.StatusCode == HttpStatusCode.Created).ShouldBeGreaterThanOrEqualTo(1);

            foreach (var refused in responses.Where(response => response.StatusCode != HttpStatusCode.Created))
            {
                refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            }

            var stored = await Factory.WithDbAsync(db => db.Reservations.CountAsync(r => r.CarId == car.Id));
            stored.ShouldBe(1, "one key means one booking, however many requests carried it");
        }
        finally
        {
            clients.ForEach(client => client.Dispose());
        }
    }
}
