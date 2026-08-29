using System.Net;
using CarRental.Domain.Entities;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

/// <summary>
/// Rotation is the one race in the system that a stolen credential can be made to exploit, so it
/// gets the same treatment the booking race got.
/// </summary>
public sealed class RefreshRotationTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    private const int Racers = 8;

    [Fact]
    public async Task Refresh_WhenManyRequestsCarryOneToken_MintsExactlyOnePair()
    {
        var auth = await SignUpAsync();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, Racers).Select(_ => RefreshWithAsync(auth.RefreshToken)));

        responses.Count(response => response.StatusCode == HttpStatusCode.OK)
            .ShouldBe(1, "one token must buy one pair, however many requests carry it");

        foreach (var loser in responses.Where(response => response.StatusCode != HttpStatusCode.OK))
        {
            loser.ShouldBeUnauthorized(
                AuthErrors.InvalidRefreshToken,
                "losing a race is not evidence of theft, and must not sign the user out");
        }

        var live = await Factory.WithDbAsync(db => db.RefreshTokens
            .CountAsync(token => token.UserId == auth.User.Id && token.RevokedAtUtc == null));

        live.ShouldBe(1, "one live token family, not several");
    }

    [Fact]
    public async Task Refresh_WhenASpentTokenIsPresentedAgain_EndsEverySessionTheUserHas()
    {
        var registration = TestData.Registration();
        var auth = await SignUpAsync(registration);

        var rotated = (await RefreshWithAsync(auth.RefreshToken)).RefreshCookie.ShouldNotBeNull();

        // Past the window in which a second use is a race rather than a replay.
        await Task.Delay(TimeSpan.FromMilliseconds(1300));

        (await RefreshWithAsync(auth.RefreshToken)).ShouldBeUnauthorized(AuthErrors.RefreshTokenReused);

        (await RefreshWithAsync(rotated)).ShouldBeUnauthorized(AuthErrors.InvalidRefreshToken,
            "the replacement must die with the family, or the thief keeps it");

        var live = await Factory.WithDbAsync(db => db.RefreshTokens
            .CountAsync(token => token.UserId == auth.User.Id && token.RevokedAtUtc == null));

        live.ShouldBe(0);
    }

    [Fact]
    public async Task Refresh_WhenSpent_RecordsWhatReplacedIt()
    {
        var auth = await SignUpAsync();

        (await RefreshWithAsync(auth.RefreshToken)).ShouldBeOk();

        var spent = await Factory.WithDbAsync(db => db.RefreshTokens
            .SingleAsync(token => token.TokenHash == RefreshToken.HashOf(auth.RefreshToken)));

        spent.RevokedAtUtc.ShouldNotBeNull();
        spent.ReplacedByTokenId.ShouldNotBeNull("without this a replay cannot be told from an ordinary revocation");

        var replacement = await Factory.WithDbAsync(db => db.RefreshTokens
            .SingleAsync(token => token.Id == spent.ReplacedByTokenId));

        replacement.RevokedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task Refresh_AfterLoggingOut_IsNotMistakenForATheft()
    {
        var auth = await SignUpAsync();

        (await Api.Auth.LogoutAsync()).ShouldBeNoContent();

        (await RefreshWithAsync(auth.RefreshToken)).ShouldBeUnauthorized(
            AuthErrors.InvalidRefreshToken,
            "a token revoked by signing out was never spent, so it is not a replay");
    }

    [Fact]
    public async Task Refresh_AfterAPasswordChange_IsNotMistakenForATheft()
    {
        var registration = TestData.Registration();
        var auth = await SignUpAsync(registration);

        (await Api.Profile.ChangePasswordAsync(TestData.Password, TestData.NewPassword)).ShouldBeNoContent();

        (await RefreshWithAsync(auth.RefreshToken)).ShouldBeUnauthorized(AuthErrors.InvalidRefreshToken);
    }
}
