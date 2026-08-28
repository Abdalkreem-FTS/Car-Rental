using CarRental.Application.Contracts.Profile;
using CarRental.Application.Mapping;
using CarRental.Domain.Entities;
using Shouldly;

namespace CarRental.UnitTests;

public sealed class ProfileMappingTests
{
    private static UpdateProfileRequest Request(
        string? addressLine2 = "Apt 5",
        string licence = "jo-1234",
        string firstName = "Lina") => new(
        FirstName: firstName,
        LastName: "Haddad",
        PhoneNumber: "+962791234567",
        DateOfBirth: new DateOnly(1996, 4, 12),
        AddressLine1: "22 Wasfi Al-Tal St",
        AddressLine2: addressLine2,
        City: "Amman",
        Country: "Jordan",
        DriverLicenseNumber: licence);

    private static ApplicationUser User() => new()
    {
        FirstName = "Old",
        LastName = "Name",
        AddressLine1 = "Old address",
        City = "Old city",
        Country = "Old country",
        DriverLicenseNumber = "OLD-1",
    };

    [Fact]
    public void ApplyTo_WhenCalled_CopiesEveryFieldOnToTheUser()
    {
        var user = User();

        Request().ApplyTo(user);

        user.FirstName.ShouldBe("Lina");
        user.LastName.ShouldBe("Haddad");
        user.PhoneNumber.ShouldBe("+962791234567");
        user.DateOfBirth.ShouldBe(new DateOnly(1996, 4, 12));
        user.AddressLine1.ShouldBe("22 Wasfi Al-Tal St");
        user.AddressLine2.ShouldBe("Apt 5");
        user.City.ShouldBe("Amman");
        user.Country.ShouldBe("Jordan");
    }

    [Fact]
    public void ApplyTo_WithSurroundingSpace_TrimsEveryText()
    {
        var user = User();

        Request(addressLine2: "  Apt 5  ", firstName: "  Lina  ").ApplyTo(user);

        user.FirstName.ShouldBe("Lina");
        user.AddressLine2.ShouldBe("Apt 5");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyTo_WithABlankSecondAddressLine_StoresNothingRatherThanBlank(string? addressLine2)
    {
        var user = User();

        Request(addressLine2: addressLine2).ApplyTo(user);

        user.AddressLine2.ShouldBeNull();
    }

    [Fact]
    public void ApplyTo_WithALicence_UppercasesItSoItComparesConsistently()
    {
        var user = User();

        Request(licence: "  jo-1234  ").ApplyTo(user);

        user.DriverLicenseNumber.ShouldBe("JO-1234");
    }

    [Fact]
    public void ApplyTo_WhenCalled_LeavesIdentityAndAuditFieldsAlone()
    {
        var user = User();
        var id = user.Id;
        var createdAt = user.CreatedAtUtc;
        user.Email = "someone@example.com";

        Request().ApplyTo(user);

        user.Id.ShouldBe(id);
        user.CreatedAtUtc.ShouldBe(createdAt);
        user.Email.ShouldBe("someone@example.com");
    }
}
