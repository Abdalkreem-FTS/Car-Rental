using System.Net;
using CarRental.Application.Contracts.Auth;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class AccountConcurrencyTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    private const int Racers = 8;

    [Fact]
    public async Task Register_WhenManyRequestsRaceForTheSameEmail_LetsExactlyOneThrough()
    {
        var email = TestData.UniqueEmail();

        var responses = await RaceAsync(_ => TestData.Registration() with { Email = email });

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).ShouldBe(1);

        foreach (var loser in responses.Where(response => response.StatusCode != HttpStatusCode.OK))
        {
            loser.ShouldBeConflict(UserErrors.EmailAlreadyInUse());
        }

        var stored = await Factory.WithDbAsync(db => db.Users.CountAsync(user => user.Email == email));
        stored.ShouldBe(1, "the database must hold one account, whatever the API replied");
    }

    [Fact]
    public async Task Register_WhenManyRequestsRaceForTheSameLicence_LetsExactlyOneThrough()
    {
        var licence = TestData.UniqueLicense();

        var responses = await RaceAsync(_ => TestData.Registration() with { DriverLicenseNumber = licence });

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).ShouldBe(1);

        foreach (var loser in responses.Where(response => response.StatusCode != HttpStatusCode.OK))
        {
            loser.ShouldBeConflict(UserErrors.LicenseAlreadyInUse);
        }

        var stored = await Factory.WithDbAsync(db =>
            db.Users.CountAsync(user => user.DriverLicenseNumber == licence));

        stored.ShouldBe(1);
    }

    [Fact]
    public async Task Register_WhenRacingRequestsShareNothing_AllSucceed()
    {
        var responses = await RaceAsync(_ => TestData.Registration());

        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithALicenceAlreadyTaken_StillReportsTheConflictWhenUncontended()
    {
        var first = TestData.Registration();
        (await Api.Auth.RegisterAsync(first)).ShouldBeOk();

        var response = await Api.Auth.RegisterAsync(
            TestData.Registration() with { DriverLicenseNumber = first.DriverLicenseNumber });

        response.ShouldBeConflict(UserErrors.LicenseAlreadyInUse);
    }

    [Fact]
    public async Task CreateCar_WhenManyRequestsRaceForTheSamePlate_LetsExactlyOneThrough()
    {
        var accessToken = (await SignInAsAdminAsync()).AccessToken;
        var plate = TestData.UniquePlate();

        var clients = Enumerable.Range(0, Racers).Select(_ => AuthenticatedClient(accessToken)).ToList();

        try
        {
            var responses = await Task.WhenAll(clients.Select(client =>
                client.Cars.CreateAsync(TestData.NewCar() with { PlateNumber = plate })));

            responses.Count(response => response.StatusCode == HttpStatusCode.Created).ShouldBe(1);

            foreach (var loser in responses.Where(response => response.StatusCode != HttpStatusCode.Created))
            {
                loser.ShouldBeConflict(CarErrors.PlateAlreadyInUse());
            }

            var stored = await Factory.WithDbAsync(db => db.Cars.CountAsync(car => car.PlateNumber == plate));
            stored.ShouldBe(1);
        }
        finally
        {
            clients.ForEach(client => client.Dispose());
        }
    }

    [Fact]
    public async Task UpdateProfile_WhenManyAccountsRaceForTheSameLicence_LetsExactlyOneThrough()
    {
        var licence = TestData.UniqueLicense();
        var clients = new List<CarRentalApi>();

        try
        {
            for (var i = 0; i < Racers; i++)
            {
                var client = new CarRentalApi(Factory.CreateClient());
                var auth = (await client.Auth.RegisterAsync(TestData.Registration())).ShouldBeOk();

                client.Authenticate(auth.AccessToken);
                clients.Add(client);
            }

            var responses = await Task.WhenAll(clients.Select(client =>
                client.Profile.UpdateAsync(TestData.ProfileUpdate() with { DriverLicenseNumber = licence })));

            responses.Count(response => response.StatusCode == HttpStatusCode.OK).ShouldBe(1);

            foreach (var loser in responses.Where(response => response.StatusCode != HttpStatusCode.OK))
            {
                loser.ShouldBeConflict(UserErrors.LicenseAlreadyInUse);
            }

            var stored = await Factory.WithDbAsync(db =>
                db.Users.CountAsync(user => user.DriverLicenseNumber == licence));

            stored.ShouldBe(1);
        }
        finally
        {
            clients.ForEach(client => client.Dispose());
        }
    }

    private async Task<IReadOnlyList<ApiResponse<AuthResponse>>> RaceAsync(Func<int, RegisterRequest> registration)
    {
        var clients = Enumerable.Range(0, Racers).Select(_ => new CarRentalApi(Factory.CreateClient())).ToList();

        try
        {
            return await Task.WhenAll(clients.Select((client, index) => client.Auth.RegisterAsync(registration(index))));
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
