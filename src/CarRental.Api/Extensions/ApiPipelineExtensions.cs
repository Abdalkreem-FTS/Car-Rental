using CarRental.Api.Endpoints;
using Scalar.AspNetCore;

namespace CarRental.Api.Extensions;

public static class ApiPipelineExtensions
{
    extension(WebApplication app)
    {
        public WebApplication UseApiPipeline()
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseSecurityHeaders();

            app.UseExceptionHandler();
            app.UseStatusCodePages();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi().AllowAnonymous();
                app.MapScalarApiReference(options => options.WithTitle("Car Rental API")).AllowAnonymous();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();

            app.MapApiEndpoints();

            return app;
        }

        private WebApplication MapApiEndpoints()
        {
            app.MapAuthEndpoints();
            app.MapCarEndpoints();
            app.MapReservationEndpoints();
            app.MapProfileEndpoints();
            app.MapCountryEndpoints();

            app.MapHealthEndpoint();

            return app;
        }
    }
}
