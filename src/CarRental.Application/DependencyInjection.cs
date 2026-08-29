using CarRental.Application.Abstractions;
using CarRental.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarRental.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ITokenIssuer, TokenIssuer>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<ICarService, CarService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IProfileService, ProfileService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
