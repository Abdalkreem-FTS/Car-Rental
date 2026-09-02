using System.Net;
using System.Text;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Shouldly;

namespace CarRental.IntegrationTests.Contracts;

public sealed class ProblemShapeTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Problem_ForAnUnknownRoute_CarriesAnErrorCodeTheClientCanSwitchOn()
    {
        await SignUpAsync();

        var response = await Api.SendAsync(HttpMethod.Get, "/api/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Problem!.ErrorCode.ShouldBe(RequestErrors.NoSuchEndpoint.Code);
    }

    [Fact]
    public async Task Problem_ForAnUnsupportedMethod_CarriesAnErrorCode()
    {
        await SignUpAsync();

        var response = await Api.SendAsync(HttpMethod.Delete, Routes.Health);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        response.Problem!.ErrorCode.ShouldBe(RequestErrors.MethodNotAllowed.Code);
    }

    [Fact]
    public async Task Problem_ForMalformedJson_CarriesAnErrorCode()
    {
        var response = await Api.PostRawAsync(Routes.Auth.Login, "{\"email\":");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Problem!.ErrorCode.ShouldBe(RequestErrors.Malformed.Code);
        response.Problem.Errors.ShouldBeNull("a document must not carry both conventions at once");
    }

    [Fact]
    public async Task Problem_ForAValidationFailure_CarriesErrorsAndNoErrorCode()
    {
        var response = await Api.Auth.ForgotPasswordAsync(string.Empty);

        response.Problem!.Errors.ShouldNotBeNull();
        response.Problem.ErrorCode.ShouldBeNull("a document must not carry both conventions at once");
    }

    [Fact]
    public async Task Problem_ForAnUnsupportedMediaType_CarriesAnErrorCode()
    {
        await SignUpAsync();

        var response = await Api.Http.PostAsync(
            Routes.Reservations.Base,
            new StringContent("not json", Encoding.UTF8, "text/plain"));

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain(RequestErrors.UnsupportedMediaType.Code);
    }
}
