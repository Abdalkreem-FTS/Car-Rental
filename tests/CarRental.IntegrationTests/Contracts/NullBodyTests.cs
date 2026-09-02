using System.Net;
using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Shouldly;

namespace CarRental.IntegrationTests.Contracts;

public sealed class NullBodyTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/register")]
    [InlineData("/api/auth/forgot-password")]
    public async Task Post_WithALiteralNullBody_IsRefusedRatherThanReachingTheService(string route)
    {
        var response = await Api.PostRawAsync(route, "null");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, $"body was: {response.RawBody}");
        response.Problem!.ErrorCode.ShouldNotBeNullOrWhiteSpace("the client needs something to branch on");
    }

    [Fact]
    public async Task CreateReservation_WithALiteralNullBody_IsRefusedRatherThanReachingTheService()
    {
        await SignUpAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, Routes.Reservations.Base)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var response = await Api.Http.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, $"body was: {await response.Content.ReadAsStringAsync()}");
    }
}
