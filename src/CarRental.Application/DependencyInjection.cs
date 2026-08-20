using CarRental.Application.Abstractions;
using CarRental.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CarRental.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICarService, CarService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IProfileService, ProfileService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
