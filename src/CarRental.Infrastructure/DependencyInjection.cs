using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Authentication;
using CarRental.Infrastructure.Email;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace CarRental.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("Default")));

            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;

                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.AllowedForNewUsers = true;
                })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            
            services.Configure<DataProtectionTokenProviderOptions>(options =>
                options.TokenLifespan = TimeSpan.FromHours(1));

            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<SmtpOptions>()
                .Bind(configuration.GetSection(SmtpOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

            // Sieve rejects an unknown property instead of quietly dropping that part of the
            // request, so a client typo surfaces as a 400 rather than a wrong result set.
            services.Configure<SieveOptions>(options =>
            {
                options.CaseSensitive = false;
                options.ThrowExceptions = true;
            });

            services.AddScoped<ISieveProcessor, CarSieveProcessor>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserAccountStore, UserAccountStore>();
            services.AddScoped<ICarRepository, CarRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

            services.AddEmailSender();

            services.AddScoped<DatabaseSeeder>();

            return services;
        }
        
        private IServiceCollection AddEmailSender() =>
            services.AddScoped<IEmailSender>(provider =>
            {
                var smtp = provider.GetRequiredService<IOptions<SmtpOptions>>().Value;

                return smtp.IsConfigured
                    ? ActivatorUtilities.CreateInstance<SmtpEmailSender>(provider)
                    : ActivatorUtilities.CreateInstance<LoggingEmailSender>(provider);
            });
    }
}
