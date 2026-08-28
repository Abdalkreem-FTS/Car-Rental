using CarRental.Api.Endpoints;
using Scalar.AspNetCore;

namespace CarRental.Api.Extensions;

public static class ApiPipelineExtensions
{
    extension(WebApplication app)
    {
        public WebApplication UseApiPipeline()
        {
            app.UseExceptionHandler();
            app.UseStatusCodePages();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options => options.WithTitle("Car Rental API"));
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
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

            app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }))
                .WithTags("Diagnostics")
                .AllowAnonymous();

            return app;
        }
    }
}
