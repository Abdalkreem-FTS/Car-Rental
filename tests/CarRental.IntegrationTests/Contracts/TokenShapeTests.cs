using System.IdentityModel.Tokens.Jwt;
using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Shouldly;

namespace CarRental.IntegrationTests.Contracts;

public sealed class TokenShapeTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task AccessToken_CarriesRolesUnderTheShortClaimName()
    {
        var auth = await SignInAsAdminAsync();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(auth.AccessToken);

        token.Claims.ShouldContain(claim => claim.Type == "role" && claim.Value == "Admin");

        token.Claims.ShouldNotContain(
            claim => claim.Type.StartsWith("http://schemas.microsoft.com/ws/2008/06/identity/claims/"),
            "the xml schema role uri costs 54 characters in every request header");
    }

    [Fact]
    public async Task AccessToken_StampsOneMomentAcrossIssuedAtNotBeforeAndExpiry()
    {
        var auth = await SignUpAsync();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(auth.AccessToken);

        var issuedAt = long.Parse(token.Claims.Single(claim => claim.Type == "iat").Value);
        var notBefore = long.Parse(token.Claims.Single(claim => claim.Type == "nbf").Value);

        notBefore.ShouldBe(issuedAt, "one clock read, so these cannot straddle a tick");
    }

    [Fact]
    public async Task AdminEndpoints_StillAuthoriseOnTheShortRoleClaim()
    {
        await SignInAsAdminAsync();

        (await Api.Cars.CreateAsync(TestData.NewCar())).ShouldBeCreated();
    }
}
