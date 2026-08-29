using System.Reflection;
using CarRental.Api.Extensions;
using CarRental.IntegrationTests.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CarRental.IntegrationTests.Contracts;

/// <summary>
/// WithValidation is opt-in per endpoint, so forgetting it leaves an endpoint unvalidated with no
/// compile error and no failing test. This is that failing test.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ValidationCoverageTests(CarRentalApiFactory factory)
{
    [Fact]
    public void EveryEndpoint_TakingARequestWeKnowHowToValidate_Validates()
    {
        var unguarded = new List<string>();

        foreach (var (endpoint, handler) in Handlers())
        {
            var validated = endpoint.Metadata.GetOrderedMetadata<ValidatedRequest>()
                .Select(metadata => metadata.RequestType)
                .ToList();

            unguarded.AddRange(
                from parameter in handler.GetParameters()
                    .Where(parameter => HasValidator(parameter.ParameterType))
                where
                    !validated.Contains(parameter.ParameterType)
                select $"{endpoint.DisplayName} takes {parameter.ParameterType.Name} unvalidated");
        }

        unguarded.ShouldBeEmpty(
            "a request type with a validator must go through WithValidation:" + Environment.NewLine +
            string.Join(Environment.NewLine, unguarded));
    }

    [Fact]
    public void EveryValidatedEndpoint_NamesARequestItActuallyTakes()
    {
        foreach (var (endpoint, handler) in Handlers())
        {
            var parameters = handler.GetParameters().Select(parameter => parameter.ParameterType).ToList();

            foreach (var validated in endpoint.Metadata.GetOrderedMetadata<ValidatedRequest>())
            {
                parameters.ShouldContain(
                    validated.RequestType,
                    $"{endpoint.DisplayName} validates {validated.RequestType.Name}, which it does not take");
            }
        }
    }

    [Fact]
    public void TheSuite_FindsEnoughEndpointsToBeWorthTrusting()
    {
        Handlers().Count.ShouldBeGreaterThan(15);
    }

    private List<(Microsoft.AspNetCore.Http.Endpoint Endpoint, MethodInfo Handler)> Handlers() =>
    [
        .. factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(endpoint => (endpoint, handler: endpoint.Metadata.OfType<MethodInfo>().LastOrDefault()))
            .Where(pair => pair.handler is not null)
            .Select(pair => (pair.endpoint, pair.handler!)),
    ];

    private bool HasValidator(Type requestType) =>
        !requestType.IsPrimitive
        && factory.Services.GetRequiredService<IServiceProviderIsService>()
            .IsService(typeof(IValidator<>).MakeGenericType(requestType));
}
