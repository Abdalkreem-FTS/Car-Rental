using CarRental.Application.Contracts.Auth;
using CarRental.Application.Contracts.Cars;
using CarRental.Domain.Entities;
using CarRental.IntegrationTests.Api;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarRental.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase(CarRentalApiFactory factory) : IAsyncLifetime
{
    protected CarRentalApiFactory Factory { get; } = factory;

    protected CarRentalApi Api { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Factory.ResetAsync();

        Api = new CarRentalApi(Factory.CreateClient());
    }

    public Task DisposeAsync()
    {
        Api.Dispose();

        return Task.CompletedTask;
    }

    protected async Task<AuthResponse> SignUpAsync(RegisterRequest? registration = null)
    {
        var auth = (await Api.Auth.RegisterAsync(registration ?? TestData.Registration())).ShouldBeCreated();

        Api.Authenticate(auth.AccessToken);

        return auth;
    }

    protected async Task<AuthResponse> SignInAsync(string email, string password)
    {
        var auth = (await Api.Auth.LoginAsync(email, password)).ShouldBeOk();

        Api.Authenticate(auth.AccessToken);

        return auth;
    }

    protected Task<AuthResponse> SignInAsAdminAsync() => SignInAsync(TestData.AdminEmail, TestData.AdminPassword);

    protected void SignOut() => Api.SignOut();
    
    protected async Task<CarResponse> FindCarAsync(string query)
    {
        var page = (await Api.Cars.SearchAsync(CarQuery.Matching(query))).ShouldBeOk();

        return page.Items.ShouldHaveSingleItem();
    }
    
    protected Task<ApplicationUser> StoredUserAsync(string email)
    {
        var normalizedEmail = email.ToUpperInvariant();

        return Factory.WithDbAsync(db => db.Users
            .AsNoTracking()
            .SingleAsync(user => user.NormalizedEmail == normalizedEmail));
    }

    protected async Task LockOutAsync(string email)
    {
        for (var attempt = 0; attempt < TestData.MaxFailedSignIns; attempt++)
        {
            await Api.Auth.LoginAsync(email, "Wr0ng#Pass1");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<CarRentalApiFactory>
{
    public const string Name = "api";
}
