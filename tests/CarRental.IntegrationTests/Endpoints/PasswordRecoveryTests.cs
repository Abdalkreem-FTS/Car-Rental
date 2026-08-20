using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Shouldly;
using static CarRental.IntegrationTests.Infrastructure.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class PasswordRecoveryTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ForgotPassword_ForAKnownOrUnknownAddress_AnswersIdentically()
    {
        var auth = await SignUpAsync();
        SignOut();

        var known = await Api.Auth.ForgotPasswordAsync(auth.User.Email);
        var unknown = await Api.Auth.ForgotPasswordAsync(UniqueEmail());

        known.ShouldBeAccepted();
        unknown.ShouldBeAccepted();
        unknown.RawBody.ShouldBe(known.RawBody);

        Factory.Emails.Sent.ShouldHaveSingleItem().Email.ShouldBe(auth.User.Email);
    }

    [Fact]
    public async Task ForgotPassword_WhenALinkIsIssued_SendsAnOpaqueTokenRatherThanAnythingReadable()
    {
        var (email, token) = await RequestResetAsync();

        token.ShouldNotBeNullOrWhiteSpace();
        token.ShouldNotContain(email);
        Uri.EscapeDataString(token).ShouldBe(token, "the token must survive a query string untouched");
    }

    [Fact]
    public async Task ForgotPassword_WhenBuildingTheLink_UsesTheConfiguredClientOriginNotTheRequestHost()
    {
        var auth = await SignUpAsync();
        SignOut();

        (await Api.Auth.ForgotPasswordAsync(auth.User.Email)).ShouldBeAccepted();

        var link = Factory.Emails.LinkFor(auth.User.Email);
        
        link.ShouldStartWith("https://rentals.example.test/reset-password.html?");
        link.ShouldContain($"email={Uri.EscapeDataString(auth.User.Email)}");
    }

    [Fact]
    public async Task ResetPassword_WithAValidToken_SetsTheNewPasswordAndRetiresTheOld()
    {
        var (email, token) = await RequestResetAsync();

        (await Api.Auth.ResetPasswordAsync(email, token, NewPassword)).ShouldBeNoContent();

        (await Api.Auth.LoginAsync(email, NewPassword)).ShouldBeOk();
        (await Api.Auth.LoginAsync(email, Password)).ShouldBeUnauthorized(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task ResetPassword_WhenTheSameTokenIsUsedTwice_RejectsTheSecondAttempt()
    {
        var (email, token) = await RequestResetAsync();

        (await Api.Auth.ResetPasswordAsync(email, token, NewPassword)).ShouldBeNoContent();

        (await Api.Auth.ResetPasswordAsync(email, token, "An0ther#Pass1")).ShouldFailValidationOn("token");
    }

    [Fact]
    public async Task ResetPassword_WithATokenIssuedForAnotherAccount_IsRejected()
    {
        var (_, token) = await RequestResetAsync();

        var victim = Registration();
        await SignUpAsync(victim);
        SignOut();

        (await Api.Auth.ResetPasswordAsync(victim.Email, token, NewPassword)).ShouldFailValidationOn("token");

        (await Api.Auth.LoginAsync(victim.Email, Password)).ShouldBeOk();
    }

    [Fact]
    public async Task ResetPassword_WithATamperedToken_IsRejected()
    {
        var (email, token) = await RequestResetAsync();

        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        (await Api.Auth.ResetPasswordAsync(email, tampered, NewPassword)).ShouldFailValidationOn("token");
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("!!!!")]
    [InlineData("")]
    public async Task ResetPassword_WithGarbageInPlaceOfAToken_IsRejectedRatherThanThrowing(string nonsense)
    {
        var (email, _) = await RequestResetAsync();

        (await Api.Auth.ResetPasswordAsync(email, nonsense, NewPassword)).ShouldFailValidationOn("token");
    }

    [Fact]
    public async Task ResetPassword_WhenItSucceeds_RevokesSessionsOpenedBeforeIt()
    {
        var auth = await SignUpAsync();
        var token = await RequestResetForAsync(auth.User.Email);

        (await Api.Auth.ResetPasswordAsync(auth.User.Email, token, NewPassword)).ShouldBeNoContent();

        (await Api.Auth.RefreshAsync(auth.RefreshToken)).ShouldBeUnauthorized(AuthErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task ResetPassword_ForALockedOutAccount_ClearsTheLockout()
    {
        var auth = await SignUpAsync();
        SignOut();
        await LockOutAsync(auth.User.Email);

        var token = await RequestResetForAsync(auth.User.Email);
        (await Api.Auth.ResetPasswordAsync(auth.User.Email, token, NewPassword)).ShouldBeNoContent();

        var user = await StoredUserAsync(auth.User.Email);

        user.LockoutEnd.ShouldBeNull();
        (await Api.Auth.LoginAsync(auth.User.Email, NewPassword)).ShouldBeOk();
    }

    [Fact]
    public async Task ResetPassword_WithAWeakNewPassword_ReportsAValidationError()
    {
        var (email, token) = await RequestResetAsync();

        (await Api.Auth.ResetPasswordAsync(email, token, "weak")).ShouldFailValidationOn("password");
    }

    private async Task<(string Email, string Token)> RequestResetAsync()
    {
        var auth = await SignUpAsync();
        SignOut();

        return (auth.User.Email, await RequestResetForAsync(auth.User.Email));
    }

    private async Task<string> RequestResetForAsync(string email)
    {
        (await Api.Auth.ForgotPasswordAsync(email)).ShouldBeAccepted();

        return Factory.Emails.TokenFor(email);
    }
}
