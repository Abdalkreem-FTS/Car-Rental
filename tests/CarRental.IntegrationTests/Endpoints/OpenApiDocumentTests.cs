using System.Net;
using System.Text.Json;
using CarRental.IntegrationTests.Support;
using Shouldly;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class OpenApiDocumentTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Document_WithoutAToken_IsFetchable()
    {
        var response = await Api.Http.GetAsync("/openapi/v1.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ScalarPage_WithoutAToken_IsReachable()
    {
        (await Api.Http.GetAsync("/scalar")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Document_ForEveryOperation_DeclaresASuccessfulResponse()
    {
        var operations = await OperationsAsync();

        operations.Count.ShouldBeGreaterThan(20);

        foreach (var (name, operation) in operations)
        {
            operation.GetProperty("responses")
                .EnumerateObject()
                .Select(response => response.Name)
                .ShouldContain(status => status.StartsWith('2'), $"{name} declares no successful response");
        }
    }

    [Fact]
    public async Task Document_ForEveryProtectedOperation_DeclaresThatItCanRefuse()
    {
        var operations = await OperationsAsync();

        var protectedPrefixes = new[] { "/api/reservations", "/api/profile" };

        var protectedOperations = operations
            .Where(entry => protectedPrefixes.Any(prefix => entry.Key.Contains(prefix, StringComparison.Ordinal)))
            .ToList();

        protectedOperations.ShouldNotBeEmpty();

        foreach (var (name, operation) in protectedOperations)
        {
            operation.GetProperty("responses")
                .EnumerateObject()
                .Select(response => response.Name)
                .ShouldContain("401", $"{name} needs a token but never says so");
        }
    }

    [Theory]
    [InlineData("/api/cars", "get", "200")]
    [InlineData("/api/admin/cars", "post", "201")]
    [InlineData("/api/cars/{id}", "get", "404")]
    [InlineData("/api/reservations", "post", "409")]
    [InlineData("/api/auth/login", "post", "400")]
    [InlineData("/api/auth/forgot-password", "post", "429")]
    public async Task Document_ForAKnownOperation_DeclaresTheResponseItCanReturn(string path, string method, string status)
    {
        var operations = await OperationsAsync();

        operations.ShouldContainKey($"{method.ToUpperInvariant()} {path}");

        operations[$"{method.ToUpperInvariant()} {path}"]
            .GetProperty("responses")
            .EnumerateObject()
            .Select(response => response.Name)
            .ShouldContain(status);
    }

    [Fact]
    public async Task Document_KeepsCustomerAndAdminOperationsUnderSeparateTags()
    {
        var operations = await OperationsAsync();

        Tags(operations["GET /api/cars"]).ShouldBe(["Cars"]);
        Tags(operations["POST /api/admin/cars"]).ShouldBe(["Cars (admin)"]);
    }

    private static List<string> Tags(JsonElement operation) =>
        [.. operation.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()!)];

    [Fact]
    public async Task Document_ForAnEndpointReturningABody_NamesTheSchema()
    {
        var operations = await OperationsAsync();

        var content = operations["GET /api/cars/{id}"]
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content");

        content.GetProperty("application/json").TryGetProperty("schema", out _).ShouldBeTrue();
    }

    private async Task<Dictionary<string, JsonElement>> OperationsAsync()
    {
        var body = await Api.Http.GetStringAsync("/openapi/v1.json");
        var document = JsonDocument.Parse(body);

        var operations = new Dictionary<string, JsonElement>();

        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                operations[$"{operation.Name.ToUpperInvariant()} {path.Name}"] = operation.Value;
            }
        }

        return operations;
    }
}
