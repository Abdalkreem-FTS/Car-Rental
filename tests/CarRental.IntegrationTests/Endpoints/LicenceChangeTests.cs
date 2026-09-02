using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Support.Api;
using CarRental.IntegrationTests.Support;
using Shouldly;
using static CarRental.IntegrationTests.Support.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class LicenceChangeTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task UpdateProfile_CorrectingTheLicenceBeforeBooking_IsAllowed()
    {
        await SignUpAsync();

        var corrected = UniqueLicense();

        var profile = (await Api.Profile.UpdateAsync(ProfileUpdate() with { DriverLicenseNumber = corrected }))
            .ShouldBeOk();

        profile.DriverLicenseNumber.ShouldBe(corrected.ToUpperInvariant());
    }

    [Fact]
    public async Task UpdateProfile_ChangingTheLicenceWhileABookingStands_IsRefused()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        var response = await Api.Profile.UpdateAsync(ProfileUpdate() with { DriverLicenseNumber = UniqueLicense() });

        response.ShouldBeConflict(UserErrors.LicenceLockedByBooking);
    }

    [Fact]
    public async Task UpdateProfile_ChangingEverythingElseWhileABookingStands_IsAllowed()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        var current = (await Api.Profile.GetAsync()).ShouldBeOk();

        var profile = (await Api.Profile.UpdateAsync(ProfileUpdate() with
        {
            DriverLicenseNumber = current.DriverLicenseNumber,
            City = "Irbid",
        })).ShouldBeOk();

        profile.City.ShouldBe("Irbid", "only the licence is locked, not the whole profile");
    }

    [Fact]
    public async Task UpdateProfile_ResendingTheSameLicenceWhileABookingStands_IsAllowed()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        var current = (await Api.Profile.GetAsync()).ShouldBeOk();

        (await Api.Profile.UpdateAsync(ProfileUpdate() with
        {
            DriverLicenseNumber = current.DriverLicenseNumber.ToLowerInvariant(),
        })).ShouldBeOk("a form that round-trips the same value differently cased is not a change");
    }

    [Fact]
    public async Task UpdateProfile_OnceTheBookingIsOver_AllowsTheLicenceToChangeAgain()
    {
        await SignUpAsync();
        var car = await FindCarAsync("Corolla");
        (await Api.Reservations.CreateAsync(Booking(car.Id, 5, 7))).ShouldBeCreated();

        Factory.Clock.Advance(TimeSpan.FromDays(30));

        (await Api.Profile.UpdateAsync(ProfileUpdate() with { DriverLicenseNumber = UniqueLicense() })).ShouldBeOk();
    }
}
