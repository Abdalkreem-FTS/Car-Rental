using System.Reflection;
using CarRental.Api.Contracts;
using CarRental.Api.Errors;
using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using FluentValidation;

namespace CarRental.Api.Filters;

public sealed record ValidatedRequest(Type RequestType);

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest, TDto>(this RouteHandlerBuilder builder)
        where TRequest : IRequestContract<TDto> =>
        builder
            .AddEndpointFilterFactory((factory, next) =>
            {
                RequireValidator<TDto>(factory.ApplicationServices);

                var index = ArgumentIndexOf<TRequest>(factory.MethodInfo);

                return context => ValidateAsync<TRequest, TDto>(context, next, index);
            })
            .WithMetadata(new ValidatedRequest(typeof(TRequest)))
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static void RequireValidator<TDto>(IServiceProvider services)
    {
        if (!services.GetRequiredService<IServiceProviderIsService>().IsService(typeof(IValidator<TDto>)))
        {
            throw new InvalidOperationException(
                $"WithValidation needs an IValidator<{typeof(TDto).Name}> registered. " +
                "Without one the endpoint would answer 500 on its first request instead of failing here.");
        }
    }

    private static int ArgumentIndexOf<TRequest>(MethodInfo handler)
    {
        var parameters = handler.GetParameters();

        var matches = parameters
            .Index()
            .Where(parameter => parameter.Item.ParameterType == typeof(TRequest))
            .Select(parameter => parameter.Index)
            .ToList();

        return matches.Count == 1
            ? matches[0]
            : throw new InvalidOperationException(
                $"WithValidation<{typeof(TRequest).Name}> needs exactly one parameter of that type on " +
                $"'{handler.Name}', but found {matches.Count}. Which one to validate would be a guess.");
    }

    private static async ValueTask<object?> ValidateAsync<TRequest, TDto>(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next,
        int index)
        where TRequest : IRequestContract<TDto>
    {
        if (context.GetArgument<TRequest>(index) is not { } request)
        {
            return RequestErrors.BodyRequired.ToProblem();
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<TDto>>();
        var validation = await validator.ValidateAsync(request.ToDto(), context.HttpContext.RequestAborted);

        if (validation.IsValid)
        {
            return await next(context);
        }

        List<Error> errors =
        [
            .. validation.Errors.Select(failure => Error.Validation(PropertyPath.ToJsonName(failure.PropertyName), failure.ErrorMessage)),
        ];

        return errors.ToProblem();
    }
}
