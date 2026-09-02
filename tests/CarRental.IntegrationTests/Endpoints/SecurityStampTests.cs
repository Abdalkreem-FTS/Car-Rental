using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class SecurityStampTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task AccessToken_AfterThePasswordIsReset_StopsWorkingImmediately()
    {
        var auth = await SignUpAsync();

        (await Api.Profile.GetAsync()).ShouldBeOk("the token works before the reset");

        (await Api.Auth.ForgotPasswordAsync(auth.User.Email)).ShouldBeAccepted();

        var token = (await Factory.DeliveredEmailsAsync()).TokenFor(auth.User.Email);

        (await Api.Auth.ResetPasswordAsync(auth.User.Email, token, "A-New-Password#98765")).ShouldBeNoContent();

        (await Api.Profile.GetAsync()).StatusCode
            .ShouldBe(System.Net.HttpStatusCode.Unauthorized,
                "a stolen access token must not outlive the password it was issued against");
    }

    [Fact]
    public async Task AccessToken_AfterThePasswordIsChanged_StopsWorkingImmediately()
    {
        await SignUpAsync();

        (await Api.Profile.ChangePasswordAsync(TestData.Password, "A-New-Password#98765")).ShouldBeNoContent();

        (await Api.Profile.GetAsync()).StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessToken_WhenNothingChanged_KeepsWorking()
    {
        await SignUpAsync();

        (await Api.Profile.GetAsync()).ShouldBeOk();
        (await Api.Profile.GetAsync()).ShouldBeOk();
    }
}
