using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class AuthEndpointTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_WithAValidForm_CreatesACustomerAndSignsThemIn()
    {
        var registration = TestData.Registration();

        var auth = (await Api.Auth.RegisterAsync(registration)).ShouldBeCreated(atLocation: "/api/profile");

        auth.AccessToken.ShouldNotBeNullOrWhiteSpace();
        auth.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        auth.ExpiresAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        auth.User.Email.ShouldBe(registration.Email);
        auth.User.Roles.ShouldBe(["Customer"]);
    }

    [Fact]
    public async Task Register_WithSeveralInvalidFields_ReportsThemAllKeyedByJsonName()
    {
        var response = await Api.Auth.RegisterAsync(TestData.Registration() with
        {
            FirstName = "",
            Email = "not-an-email",
            Password = "weak",
            ConfirmPassword = "different",
            PhoneNumber = "x",
            AddressLine1 = "",
            City = "",
            Country = "",
            DriverLicenseNumber = "a",
        });

        response.ShouldFailValidation(
            "firstName", "email", "password", "confirmPassword",
            "phoneNumber", "addressLine1", "city", "country", "driverLicenseNumber");
    }

    [Fact]
    public async Task Register_WithAPasswordMissingEveryRule_ReportsEachRuleSeparately()
    {
        var response = await Api.Auth.RegisterAsync(TestData.Registration() with
        {
            Password = "alllowercase",
            ConfirmPassword = "alllowercase",
        });

        var problem = response.ShouldFailValidationOn("password");

        problem.Errors!["password"].ShouldContain(message => message.Contains("uppercase"));
        problem.Errors["password"].ShouldContain(message => message.Contains("digit"));
        problem.Errors["password"].ShouldContain(message => message.Contains("special character"));
    }

    [Fact]
    public async Task Register_WithAnEmailAlreadyInUse_ReportsAConflictRegardlessOfCasing()
    {
        var taken = await SignUpAsync();

        var response = await Api.Auth.RegisterAsync(TestData.Registration() with
        {
            Email = taken.User.Email.ToUpperInvariant(),
        });

        response.ShouldBeConflict(UserErrors.EmailAlreadyInUse(taken.User.Email));
    }

    [Fact]
    public async Task Register_WithADriverLicenceAlreadyInUse_ReportsAConflict()
    {
        var registration = TestData.Registration();
        await SignUpAsync(registration);

        var response = await Api.Auth.RegisterAsync(TestData.Registration() with
        {
            DriverLicenseNumber = registration.DriverLicenseNumber.ToLowerInvariant(),
        });

        response.ShouldBeConflict(UserErrors.LicenseAlreadyInUse);
    }

    [Theory]
    [InlineData(3650, "a date of birth in the future")]
    [InlineData(-3650, "someone about ten years old")]
    public async Task Register_WithAnUnusableDateOfBirth_ReportsAValidationError(int daysFromToday, string _)
    {
        var response = await Api.Auth.RegisterAsync(TestData.Registration() with
        {
            DateOfBirth = TestData.In(daysFromToday),
        });

        response.ShouldFailValidationOn("dateOfBirth");
    }

    [Fact]
    public async Task Register_WithoutADateOfBirth_Succeeds()
    {
        var response = await Api.Auth.RegisterAsync(TestData.Registration() with { DateOfBirth = null });

        response.ShouldBeCreated();
    }
    
    [Fact]
    public async Task Login_WithADifferentlyCasedEmail_Succeeds()
    {
        var auth = await SignUpAsync();
        SignOut();

        var response = await Api.Auth.LoginAsync(auth.User.Email.ToUpperInvariant(), TestData.Password);

        response.ShouldBeOk();
    }

    [Fact]
    public async Task Login_WithAWrongPasswordOrAnUnknownAccount_AnswersIdentically()
    {
        var auth = await SignUpAsync();
        SignOut();

        var wrongPassword = await Api.Auth.LoginAsync(auth.User.Email, "Wr0ng#Pass1");
        var unknownAccount = await Api.Auth.LoginAsync(TestData.UniqueEmail(), "Wr0ng#Pass1");

        wrongPassword.ShouldBeUnauthorized(UserErrors.InvalidCredentials);
        unknownAccount.ShouldBeUnauthorized(UserErrors.InvalidCredentials);
        unknownAccount.Problem!.Detail.ShouldBe(wrongPassword.Problem!.Detail);
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksTheAccountEvenForTheRightPassword()
    {
        var auth = await SignUpAsync();
        SignOut();

        for (var attempt = 1; attempt < TestData.MaxFailedSignIns; attempt++)
        {
            var failure = await Api.Auth.LoginAsync(auth.User.Email, "Wr0ng#Pass1");

            failure.ShouldBeUnauthorized(UserErrors.InvalidCredentials);
        }

        (await Api.Auth.LoginAsync(auth.User.Email, "Wr0ng#Pass1")).ShouldBeForbidden(UserErrors.LockedOut);
        (await Api.Auth.LoginAsync(auth.User.Email, TestData.Password)).ShouldBeForbidden(UserErrors.LockedOut);

        var user = await StoredUserAsync(auth.User.Email);

        user.LockoutEnd.ShouldNotBeNull();
        user.AccessFailedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Login_AfterEarlierFailures_ClearsTheFailureCount()
    {
        var auth = await SignUpAsync();
        SignOut();

        await Api.Auth.LoginAsync(auth.User.Email, "Wr0ng#Pass1");
        await Api.Auth.LoginAsync(auth.User.Email, "Wr0ng#Pass1");
        (await Api.Auth.LoginAsync(auth.User.Email, TestData.Password)).ShouldBeOk();

        var user = await StoredUserAsync(auth.User.Email);

        user.AccessFailedCount.ShouldBe(0);
        user.LockoutEnd.ShouldBeNull();
    }
    
    [Fact]
    public async Task Refresh_WithAValidToken_RotatesThePairAndRevokesThePresentedToken()
    {
        var auth = await SignUpAsync();

        var rotated = (await Api.Auth.RefreshAsync(auth.RefreshToken)).ShouldBeOk();
        rotated.RefreshToken.ShouldNotBe(auth.RefreshToken);

        (await Api.Auth.RefreshAsync(auth.RefreshToken)).ShouldBeUnauthorized(AuthErrors.InvalidRefreshToken);

        (await Api.Auth.RefreshAsync(rotated.RefreshToken)).ShouldBeOk();
    }

    [Fact]
    public async Task Refresh_WithATokenThatWasNeverIssued_ReportsUnauthorized()
    {
        var response = await Api.Auth.RefreshAsync("not-a-real-token");

        response.ShouldBeUnauthorized(AuthErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task Refresh_WithAnExpiredToken_ReportsUnauthorized()
    {
        var auth = await SignUpAsync();

        await Factory.WithDbAsync(async db =>
        {
            var token = await db.RefreshTokens.SingleAsync(t => t.Token == auth.RefreshToken);
            token.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);

            return await db.SaveChangesAsync();
        });

        (await Api.Auth.RefreshAsync(auth.RefreshToken)).ShouldBeUnauthorized(AuthErrors.InvalidRefreshToken);
    }
    
    [Fact]
    public async Task Logout_WhenSignedIn_RevokesEveryRefreshTokenTheUserHolds()
    {
        var first = await SignUpAsync();
        var second = await SignInAsync(first.User.Email, TestData.Password);

        Api.Authenticate(first.AccessToken);
        (await Api.Auth.LogoutAsync()).ShouldBeNoContent();

        foreach (var refreshToken in new[] { first.RefreshToken, second.RefreshToken })
        {
            (await Api.Auth.RefreshAsync(refreshToken)).ShouldBeUnauthorized(AuthErrors.InvalidRefreshToken);
        }
    }

    [Fact]
    public async Task Logout_WithoutAToken_ReportsUnauthorized()
    {
        SignOut();

        (await Api.Auth.LogoutAsync()).ShouldRequireAuthentication();
    }
}
