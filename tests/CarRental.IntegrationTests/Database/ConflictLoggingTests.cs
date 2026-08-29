using CarRental.Application.Abstractions;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Persistence;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace CarRental.IntegrationTests.Database;

public sealed class ConflictLoggingTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task TrySaveChanges_WhenTheDatabaseRefusesTheWrite_LeavesTheConstraintNameInTheLog()
    {
        var conflict = await DuplicatePlateAsync();

        conflict.IsError.ShouldBeTrue();

        var warning = Factory.Logs.Entries
            .Where(entry => entry.Level == LogLevel.Warning)
            .ShouldHaveSingleItem();

        warning.Message.ShouldContain("IX_Cars_PlateNumber");
        warning.Message.ShouldContain(conflict.Errors.ShouldHaveSingleItem().Code);
        warning.Exception.ShouldNotBeNull("without the exception there is nothing left to diagnose from");
    }

    [Fact]
    public async Task TrySaveChanges_WhenTheWriteSucceeds_LogsNothing()
    {
        await Factory.WithDbAsync(async db =>
        {
            db.Cars.Add(Clone("CLEAN-1"));

            return await db.SaveChangesAsync();
        });

        Factory.Logs.Entries.ShouldBeEmpty();
    }

    private async Task<Result<Success>> DuplicatePlateAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        context.Cars.Add(Clone("DUPE-1"));
        await context.SaveChangesAsync();

        context.Cars.Add(Clone("DUPE-1"));

        return await unitOfWork.TrySaveChangesAsync();
    }

    private static Car Clone(string plateNumber) => new()
    {
        Make = "Clone",
        Model = "Car",
        Year = 2024,
        PlateNumber = plateNumber,
        Location = "Amman",
        DailyRate = 40m,
        Seats = 5,
    };
}
