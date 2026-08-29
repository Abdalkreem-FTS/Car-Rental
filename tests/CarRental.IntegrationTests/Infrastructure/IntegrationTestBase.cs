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

    protected async Task<AuthResponse> SignUpAsync(RegisterRequest? registration = null, bool confirmEmail = true)
    {
        var request = registration ?? TestData.Registration();
        var response = await Api.Auth.RegisterAsync(request);
        var auth = response.ShouldBeOk() with { RefreshToken = response.RefreshCookie ?? string.Empty };

        Api.Authenticate(auth.AccessToken);

        if (confirmEmail)
        {
            await ConfirmEmailAsync(request.Email);
        }

        return auth;
    }

    protected async Task ConfirmEmailAsync(string email)
    {
        var token = (await Factory.DeliveredEmailsAsync()).ConfirmationTokenFor(email);

        (await Api.Auth.ConfirmEmailAsync(email, token)).ShouldBeNoContent();
    }

    protected async Task<AuthResponse> SignInAsync(string email, string password)
    {
        var response = await Api.Auth.LoginAsync(email, password);
        var auth = response.ShouldBeOk() with { RefreshToken = response.RefreshCookie ?? string.Empty };

        Api.Authenticate(auth.AccessToken);

        return auth;
    }

    protected Task<AuthResponse> SignInAsAdminAsync() => SignInAsync(TestData.AdminEmail, TestData.AdminPassword);

    protected void SignOut() => Api.SignOut();

    protected async Task<ApiResponse<AuthResponse>> RefreshWithAsync(string refreshToken)
    {
        using var client = Factory.CreateDefaultClient();
        client.DefaultRequestHeaders.Add("Cookie", $"cr_refresh={refreshToken}");

        using var api = new CarRentalApi(client);

        return await api.Auth.RefreshAsync();
    }
    
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
