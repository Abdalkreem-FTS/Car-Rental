using CarRental.Domain.Errors;
using CarRental.IntegrationTests.Api;
using CarRental.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static CarRental.IntegrationTests.Infrastructure.TestData;

namespace CarRental.IntegrationTests.Endpoints;

public sealed class ProfileEndpointTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Profile_WithoutAToken_ReportsUnauthorized()
    {
        SignOut();

        (await Api.Profile.GetAsync()).ShouldRequireAuthentication();
        (await Api.Profile.UpdateAsync(ProfileUpdate())).ShouldRequireAuthentication();
        (await Api.Profile.ChangePasswordAsync(Password, NewPassword)).ShouldRequireAuthentication();
    }

    [Fact]
    public async Task GetProfile_WhenSignedIn_ReturnsEveryFieldTheSignUpFormCollected()
    {
        var registration = Registration();
        await SignUpAsync(registration);

        var profile = (await Api.Profile.GetAsync()).ShouldBeOk();

        profile.FirstName.ShouldBe(registration.FirstName);
        profile.LastName.ShouldBe(registration.LastName);
        profile.Email.ShouldBe(registration.Email);
        profile.PhoneNumber.ShouldBe(registration.PhoneNumber);
        profile.DateOfBirth.ShouldBe(registration.DateOfBirth);
        profile.AddressLine1.ShouldBe(registration.AddressLine1);
        profile.AddressLine2.ShouldBe(registration.AddressLine2);
        profile.City.ShouldBe(registration.City);
        profile.Country.ShouldBe(registration.Country);
        profile.DriverLicenseNumber.ShouldBe(registration.DriverLicenseNumber);
        profile.Roles.ShouldBe(["Customer"]);
        profile.CreatedAtUtc.ShouldBeInRange(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task UpdateProfile_WithValidDetails_SavesThem()
    {
        await SignUpAsync();

        var updated = (await Api.Profile.UpdateAsync(ProfileUpdate() with
        {
            FirstName = "Noora",
            City = "Irbid",
        })).ShouldBeOk();

        updated.FirstName.ShouldBe("Noora");
        updated.City.ShouldBe("Irbid");

        (await Api.Profile.GetAsync()).ShouldBeOk().FirstName.ShouldBe("Noora");
    }

    [Fact]
    public async Task UpdateProfile_WithAnEmailInTheBody_LeavesTheSignInEmailUnchanged()
    {
        var auth = await SignUpAsync();

        var update = ProfileUpdate();

        var response = await Api.PutOffContractAsync<Application.Contracts.Profile.ProfileResponse>(
            Routes.Profile.Base,
            new
            {
                email = "hijack@example.com",
                firstName = update.FirstName,
                lastName = update.LastName,
                phoneNumber = update.PhoneNumber,
                dateOfBirth = update.DateOfBirth,
                addressLine1 = update.AddressLine1,
                addressLine2 = update.AddressLine2,
                city = update.City,
                country = update.Country,
                driverLicenseNumber = update.DriverLicenseNumber,
            });

        response.ShouldBeOk().Email.ShouldBe(auth.User.Email);
    }

    [Fact]
    public async Task UpdateProfile_WithALowerCaseLicenceAndBlankAddressLine_NormalisesBoth()
    {
        await SignUpAsync();

        var updated = (await Api.Profile.UpdateAsync(ProfileUpdate() with
        {
            DriverLicenseNumber = "jo-lower-99",
            AddressLine2 = "",
        })).ShouldBeOk();

        updated.DriverLicenseNumber.ShouldBe("JO-LOWER-99");
        updated.AddressLine2.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateProfile_WithALicenceAnotherAccountHolds_ReportsAConflict()
    {
        var taken = Registration();
        await SignUpAsync(taken);

        await SignUpAsync();

        var response = await Api.Profile.UpdateAsync(ProfileUpdate() with
        {
            DriverLicenseNumber = taken.DriverLicenseNumber,
        });

        response.ShouldBeConflict(UserErrors.LicenseAlreadyInUse);
    }

    [Fact]
    public async Task UpdateProfile_KeepingItsOwnLicence_Succeeds()
    {
        var registration = Registration();
        await SignUpAsync(registration);

        var response = await Api.Profile.UpdateAsync(ProfileUpdate() with
        {
            DriverLicenseNumber = registration.DriverLicenseNumber,
        });

        response.ShouldBeOk();
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidFields_ReportsThemAll()
    {
        await SignUpAsync();

        var response = await Api.Profile.UpdateAsync(ProfileUpdate() with
        {
            FirstName = "",
            LastName = "",
            PhoneNumber = "nope",
            DateOfBirth = In(3650),
            AddressLine1 = "",
            City = "",
            Country = "",
            DriverLicenseNumber = "",
        });

        response.ShouldFailValidation(
            "firstName", "lastName", "phoneNumber", "dateOfBirth",
            "addressLine1", "city", "country", "driverLicenseNumber");
    }

    [Fact]
    public async Task ChangePassword_WithTheRightCurrentPassword_SwapsWhichOneWorks()
    {
        var auth = await SignUpAsync();

        (await Api.Profile.ChangePasswordAsync(Password, NewPassword)).ShouldBeNoContent();

        SignOut();
        (await Api.Auth.LoginAsync(auth.User.Email, NewPassword)).ShouldBeOk();
        (await Api.Auth.LoginAsync(auth.User.Email, Password)).ShouldBeUnauthorized(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task ChangePassword_WhenItSucceeds_SignsEveryOtherDeviceOut()
    {
        var firstDevice = await SignUpAsync();
        var secondDevice = await SignInAsync(firstDevice.User.Email, Password);

        (await Api.Profile.ChangePasswordAsync(Password, NewPassword)).ShouldBeNoContent();

        foreach (var refreshToken in new[] { firstDevice.RefreshToken, secondDevice.RefreshToken })
        {
            (await RefreshWithAsync(refreshToken)).ShouldBeUnauthorized(AuthErrors.InvalidRefreshToken);
        }
    }

    [Fact]
    public async Task ChangePassword_WhenItSucceeds_BurnsAnyOutstandingResetLink()
    {
        var auth = await SignUpAsync();

        (await Api.Auth.ForgotPasswordAsync(auth.User.Email)).ShouldBeAccepted();
        var resetToken = (await Factory.DeliveredEmailsAsync()).TokenFor(auth.User.Email);

        (await Api.Profile.ChangePasswordAsync(Password, NewPassword)).ShouldBeNoContent();

        var response = await Api.Auth.ResetPasswordAsync(auth.User.Email, resetToken, "Fourth#Pass88");

        response.ShouldFailValidationOn("token");
    }

    [Fact]
    public async Task ChangePassword_WithTheWrongCurrentPassword_ReportsAValidationError()
    {
        await SignUpAsync();

        var response = await Api.Profile.ChangePasswordAsync("Wr0ng#Pass1", NewPassword);

        response.ShouldFailValidationOn("currentPassword");
    }

    [Fact]
    public async Task ChangePassword_WithTheSamePasswordAgain_ReportsAValidationError()
    {
        await SignUpAsync();

        var response = await Api.Profile.ChangePasswordAsync(Password, Password);

        response.ShouldFailValidationOn("newPassword");
    }

    [Fact]
    public async Task ChangePassword_WithAWeakUnconfirmedPassword_ReportsBothProblems()
    {
        await SignUpAsync();

        var response = await Api.Profile.ChangePasswordAsync(
            new Application.Contracts.Profile.ChangePasswordRequest(Password, "weak", "mismatch"));

        response.ShouldFailValidation("newPassword", "confirmPassword");
    }

    [Fact]
    public async Task ChangePassword_WhenItSucceeds_StoresAFreshHashRatherThanTheTypedValue()
    {
        var auth = await SignUpAsync();

        var before = await HashForAsync(auth.User.Email);

        (await Api.Profile.ChangePasswordAsync(Password, NewPassword)).ShouldBeNoContent();

        var after = await HashForAsync(auth.User.Email);

        after.ShouldNotBe(before);
        after.ShouldNotContain(NewPassword);
    }

    private async Task<string> HashForAsync(string email) => (await StoredUserAsync(email)).PasswordHash!;
}
