using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class LoginDisclosureTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_ForAnUnknownAddressAndAWrongPassword_AnswersIdentically()
    {
        var auth = await SignUpAsync();
        SignOut();

        var unknown = await Api.Auth.LoginAsync("nobody@example.com", TestData.Password);
        var wrongPassword = await Api.Auth.LoginAsync(auth.User.Email, "Wr0ng#Pass1");

        ShouldBeIndistinguishable(unknown, wrongPassword);
    }

    [Fact]
    public async Task Login_WithAWrongPasswordOnALockedAccount_DoesNotRevealTheLock()
    {
        var auth = await SignUpAsync();
        SignOut();

        for (var attempt = 0; attempt < TestData.MaxFailedSignIns; attempt++)
        {
            await Api.Auth.LoginAsync(auth.User.Email, "Wr0ng#Pass1");
        }

        var locked = await Api.Auth.LoginAsync(auth.User.Email, "Wr0ng#Pass1");
        var unknown = await Api.Auth.LoginAsync("nobody@example.com", "Wr0ng#Pass1");

        locked.ShouldBeUnauthorized(UserErrors.InvalidCredentials);

        ShouldBeIndistinguishable(locked, unknown);
    }

    [Fact]
    public async Task Login_WithTheRightPasswordOnALockedAccount_SaysItIsLocked()
    {
        var auth = await SignUpAsync();
        SignOut();

        for (var attempt = 0; attempt < TestData.MaxFailedSignIns; attempt++)
        {
            await Api.Auth.LoginAsync(auth.User.Email, "Wr0ng#Pass1");
        }

        (await Api.Auth.LoginAsync(auth.User.Email, TestData.Password)).ShouldBeForbidden(UserErrors.LockedOut);
    }

    private static void ShouldBeIndistinguishable(ApiResponse one, ApiResponse other)
    {
        one.StatusCode.ShouldBe(other.StatusCode);

        var left = one.Problem.ShouldNotBeNull();
        var right = other.Problem.ShouldNotBeNull();

        left.Status.ShouldBe(right.Status);
        left.Title.ShouldBe(right.Title);
        left.Detail.ShouldBe(right.Detail, "the wording must not say which of the two happened");
        left.ErrorCode.ShouldBe(right.ErrorCode);
    }
}
