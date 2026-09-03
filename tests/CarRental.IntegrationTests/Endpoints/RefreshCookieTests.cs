using System.Net;
using System.Net.Http.Json;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class RefreshCookieTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_WhenItSignsTheUserIn_KeepsTheRefreshTokenOutOfTheBody()
    {
        var response = await Api.Auth.RegisterAsync(TestData.Registration());

        response.RawBody.ShouldNotContain("refreshToken", Case.Insensitive);
        response.RefreshCookie.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WhenItSignsTheUserIn_KeepsTheRefreshTokenOutOfTheBody()
    {
        var registration = TestData.Registration();
        await SignUpAsync(registration);
        SignOut();

        var response = await Api.Auth.LoginAsync(registration.Email, TestData.Password);

        response.RawBody.ShouldNotContain("refreshToken", Case.Insensitive);
        response.RefreshCookie.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("httponly")]
    [InlineData("samesite=strict")]
    [InlineData("path=/api/auth")]
    public async Task RefreshCookie_WhenSet_CarriesTheAttributesThatProtectIt(string attribute)
    {
        var response = await Api.Http.PostAsJsonAsync(
            Routes.Auth.Register,
            TestData.Registration(),
            CarRentalApi.Json);

        var header = response.Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith("cr_refresh=", StringComparison.Ordinal));

        header.ShouldContain(attribute, Case.Insensitive);
    }

    [Fact]
    public async Task Refresh_WithNoCookie_IsRefusedWithoutTouchingTheDatabase()
    {
        using var client = Factory.CreateDefaultClient();
        using var api = new CarRentalApi(client);

        (await api.Auth.RefreshAsync()).ShouldBeUnauthorized(AuthErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task Refresh_WhenTheCookieIsRejected_ClearsItSoTheBrowserStopsSendingIt()
    {
        var response = await RefreshWithAsync("not-a-real-token");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.RefreshCookie.ShouldBeNullOrEmpty("a rejected cookie must be cleared, not left in place");
    }

    [Fact]
    public async Task Logout_WhenSignedIn_ClearsTheRefreshCookie()
    {
        await SignUpAsync();

        var response = await Api.Auth.LogoutAsync();

        response.ShouldBeNoContent();
        response.RefreshCookie.ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_AfterAReloadWithOnlyTheCookie_MintsANewAccessToken()
    {
        var registration = TestData.Registration();
        var auth = await SignUpAsync(registration);

        // What the browser has after a reload: no access token in memory, only the cookie.
        var response = await RefreshWithAsync(auth.RefreshToken);

        response.ShouldBeOk().AccessToken.ShouldNotBeNullOrWhiteSpace();
    }
}
