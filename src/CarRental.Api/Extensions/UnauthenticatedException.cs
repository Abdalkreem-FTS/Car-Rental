using CarRental.Domain.Common;
using CarRental.Domain.Errors;

namespace CarRental.Api.Extensions;

public sealed class UnauthenticatedException() : Exception(AuthErrors.NotAuthenticated.Description)
{
    public Error Error => AuthErrors.NotAuthenticated;
}
