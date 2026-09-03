using System.Net;
using CarRental.IntegrationTests.Support;
using CarRental.IntegrationTests.Support.Api;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class SecurityHeaderTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData("/api/health")]
    [InlineData("/index.html")]
    [InlineData("/css/app.css")]
    [InlineData("/js/main.js")]
    public async Task Response_WhateverIsServed_CarriesTheSecurityHeaders(string path)
    {
        var response = await Api.Http.GetAsync(path);

        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.GetValues("Referrer-Policy").ShouldBe(["no-referrer"]);
        response.Headers.GetValues("X-Frame-Options").ShouldBe(["DENY"]);
        response.Headers.GetValues("Content-Security-Policy").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Response_WhenTheRequestFails_StillCarriesTheSecurityHeaders()
    {
        var response = await Api.Http.GetAsync("/api/reservations");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.GetValues("Referrer-Policy").ShouldBe(["no-referrer"]);
    }

    [Fact]
    public async Task Response_WrittenByTheExceptionHandler_StillCarriesTheSecurityHeaders()
    {
        var response = await Api.Http.PostAsync(
            "/api/auth/login",
            new StringContent("{\"email\":", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Headers.GetValues("Referrer-Policy").ShouldBe(["no-referrer"]);
        response.Headers.GetValues("Content-Security-Policy").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ContentSecurityPolicy_WhenServed_AllowsNothingInlineAndNoOtherOrigin()
    {
        var response = await Api.Http.GetAsync("/index.html");
        var policy = response.Headers.GetValues("Content-Security-Policy").Single();

        policy.ShouldNotContain("unsafe-inline");
        policy.ShouldNotContain("unsafe-eval");
        policy.ShouldContain("default-src 'self'");
        policy.ShouldContain("frame-ancestors 'none'");
        policy.ShouldContain("object-src 'none'");
    }
}
