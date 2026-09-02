using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class LoginWriteTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_WithNothingToReset_DoesNotWriteToTheUsersTable()
    {
        var registration = TestData.Registration();
        var auth = await SignUpAsync(registration);
        SignOut();

        var before = (await StoredUserAsync(auth.User.Email)).ConcurrencyStamp;

        (await Api.Auth.LoginAsync(registration.Email, TestData.Password)).ShouldBeOk();

        var after = (await StoredUserAsync(auth.User.Email)).ConcurrencyStamp;

        after.ShouldBe(before, "a sign-in with no failures to clear should not touch the row");
    }

    [Fact]
    public async Task Login_AfterAFailedAttempt_ClearsTheCount()
    {
        var registration = TestData.Registration();
        var auth = await SignUpAsync(registration);
        SignOut();

        (await Api.Auth.LoginAsync(registration.Email, "Wr0ng#Pass1")).StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);

        (await StoredUserAsync(auth.User.Email)).AccessFailedCount.ShouldBe(1);

        (await Api.Auth.LoginAsync(registration.Email, TestData.Password)).ShouldBeOk();

        (await StoredUserAsync(auth.User.Email)).AccessFailedCount.ShouldBe(0);
    }
}
