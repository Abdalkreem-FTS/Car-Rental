using CarRental.Domain.Common;
using FluentValidation;

namespace CarRental.Api.Extensions;

public sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
        {
            return await next(context);
        }

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

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<TRequest>>();
}
