using CarRental.Api.Extensions;
using CarRental.Application;
using CarRental.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseApiPipeline();

await app.SeedDatabaseAsync();

app.Run();
