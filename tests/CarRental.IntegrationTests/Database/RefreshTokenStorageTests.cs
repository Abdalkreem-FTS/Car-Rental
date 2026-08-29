using CarRental.Domain.Entities;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarRental.IntegrationTests.Database;

public sealed class RefreshTokenStorageTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_WhenATokenIsIssued_StoresOnlyItsHash()
    {
        var auth = await SignUpAsync();

        var stored = await Factory.WithDbAsync(db => db.RefreshTokens
            .Where(token => token.UserId == auth.User.Id)
            .Select(token => token.TokenHash)
            .ToListAsync());

        stored.ShouldNotBeEmpty();
        stored.ShouldNotContain(auth.RefreshToken, "a backup of this table must not be a set of live sessions");
        stored.ShouldContain(RefreshToken.HashOf(auth.RefreshToken));
    }

    [Fact]
    public async Task Refresh_WhenPresentedTheRawToken_StillFindsItByHash()
    {
        var auth = await SignUpAsync();

        (await RefreshWithAsync(auth.RefreshToken)).ShouldBeOk();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("a-refresh-token-with-punctuation_and-digits-0123456789")]
    public async Task Hashing_InTheDatabaseAndInTheApplication_Agree(string token)
    {
        // The migration rehashes existing rows with Postgres' sha256. If the two ever disagreed,
        // every session in flight at deploy time would be silently unusable.
        var inPostgres = await Factory.Database.QuerySingleAsync<string>(
            $"SELECT encode(sha256(convert_to('{token}', 'UTF8')), 'hex')");

        inPostgres.ShouldBe(RefreshToken.HashOf(token));
    }
}
