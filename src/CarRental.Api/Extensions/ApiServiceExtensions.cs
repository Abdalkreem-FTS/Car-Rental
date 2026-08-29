using System.Text;
using System.Text.Json.Serialization;
using CarRental.Application.Options;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CarRental.Api.Extensions;

public static class ApiServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiServices(IConfiguration configuration)
        {
            services.AddOptions<ClientAppOptions>()
            .Bind(configuration.GetSection(ClientAppOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl)
                    && baseUrl.Scheme is "http" or "https",
                "ClientApp:BaseUrl must be an absolute http or https URL.")
            .ValidateOnStart();

            services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemExtensions.Customize);
            services.AddExceptionHandler<GlobalExceptionHandler>();

            services.AddJwtAuthentication();
            services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Admin));

            services.ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddApiRateLimiting(configuration);

            services.AddOpenApi();

            return services;
        }

        private IServiceCollection AddJwtAuthentication()
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services
                .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
                {
                    var jwt = jwtOptions.Value;

                    bearer.MapInboundClaims = false;

                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                        ClockSkew = TimeSpan.FromSeconds(30),
                        RoleClaimType = JwtTokenGenerator.RoleClaimType,
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                    };
                });

            return services;
        }
    }
}
