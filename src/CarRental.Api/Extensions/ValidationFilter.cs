using System.Reflection;
using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using FluentValidation;

namespace CarRental.Api.Extensions;

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilterFactory((factory, next) =>
            {
                var index = ArgumentIndexOf<TRequest>(factory.MethodInfo);

                return context => ValidateAsync<TRequest>(context, next, index);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

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

    private static async ValueTask<object?> ValidateAsync<TRequest>(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next,
        int index)
    {
        if (context.GetArgument<TRequest>(index) is not { } request)
        {
            return RequestErrors.BodyRequired.ToProblem();
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<TRequest>>();
        var validation = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (validation.IsValid)
        {
            return await next(context);
        }

        List<Error> errors =
        [
            .. validation.Errors.Select(failure => Error.Validation(ToCamelCase(failure.PropertyName), failure.ErrorMessage)),
        ];

        return errors.ToProblem();
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0]))
        {
            return propertyName;
        }

        return string.Create(propertyName.Length, propertyName, static (span, name) =>
        {
            name.AsSpan().CopyTo(span);
            span[0] = char.ToLowerInvariant(span[0]);
        });
    }
}
