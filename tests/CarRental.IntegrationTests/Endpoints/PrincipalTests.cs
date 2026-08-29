using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class PrincipalTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    private const string SigningKey = "integration-tests-signing-key-at-least-32-characters-long";

    [Fact]
    public async Task Request_WithAValidlySignedTokenCarryingNoSubject_IsUnauthorisedRatherThanAServerError()
    {
        Api.Authenticate(TokenWithout(JwtRegisteredClaimNames.Sub));

        var response = await Api.Profile.GetAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"body was: {response.RawBody}");
        response.Problem!.ErrorCode.ShouldBe(AuthErrors.NotAuthenticated.Code);
    }

    [Fact]
    public async Task Request_WithASubjectThatIsNotAGuid_IsUnauthorisedRatherThanAServerError()
    {
        Api.Authenticate(TokenWith(new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid")));

        (await Api.Reservations.ListAsync()).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithAProperToken_StillWorks()
    {
        await SignUpAsync();

        (await Api.Profile.GetAsync()).ShouldBeOk();
    }

    private static string TokenWithout(string claimType) => TokenWith(claims: []);

    private static string TokenWith(params Claim[] claims)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "CarRental.Api",
            audience: "CarRental.Client",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
