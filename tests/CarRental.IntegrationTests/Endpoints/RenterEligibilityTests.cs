using CarRental.Domain.Errors;
using CarRental.Domain.Rules;
using CarRental.Domain;
using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class RenterEligibilityTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateReservation_WithNoDateOfBirthOnFile_IsRefused()
    {
        var registration = Registration() with { DateOfBirth = null };
        await SignUpAsync(registration);

        var car = await FindCarAsync("Corolla");

        var response = await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7));

        response.ShouldBeForbidden(UserErrors.DateOfBirthMissing);
    }

    [Fact]
    public async Task CreateReservation_BySomeoneUnderTheMinimumAge_IsRefused()
    {
        var registration = Registration();
        var auth = await SignUpAsync(registration);

        var stillSeventeen = Today.AddYears(-(RenterRules.MinimumAge - 1));

        await Factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(candidate => candidate.Id == auth.User.Id);
            user.Describe(
                user.FirstName,
                user.LastName,
                user.PhoneNumber!,
                stillSeventeen,
                user.AddressLine1,
                user.AddressLine2,
                user.City,
                user.Country,
                user.DriverLicenseNumber!);

            return await db.SaveChangesAsync();
        });

        var car = await FindCarAsync("Corolla");

        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7)))
            .ShouldBeForbidden(UserErrors.TooYoungToRent(RenterRules.MinimumAge));
    }

    [Fact]
    public async Task CreateReservation_ByAnEligibleRenter_Succeeds()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");

        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();
    }

    [Fact]
    public async Task Register_WithNoDateOfBirth_IsStillAllowed()
    {
        var response = await Api.Auth.RegisterAsync(Registration() with { DateOfBirth = null });

        response.ShouldBeOk("the gate is at booking, so signing up to browse stays open");
    }

    [Theory]
    [InlineData(2000, 1, 1, 2018, 1, 1, 18)]
    [InlineData(2000, 6, 1, 2018, 5, 31, 17)]
    [InlineData(2000, 2, 29, 2018, 2, 28, 17)]
    public void AgeOn_AcrossABirthday_CountsTheYearOnlyOnceItHasPassed(
        int birthYear, int birthMonth, int birthDay,
        int onYear, int onMonth, int onDay,
        int expected)
    {
        RenterRules.AgeOn(new DateOnly(birthYear, birthMonth, birthDay), new DateOnly(onYear, onMonth, onDay))
            .ShouldBe(expected);
    }
}
