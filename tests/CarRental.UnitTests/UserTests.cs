using CarRental.Domain.Entities;
using Shouldly;

namespace CarRental.UnitTests;

public sealed class ApplicationUserTests
{
    [Fact]
    public void FullName_ForAUser_JoinsTheTwoNameParts()
    {
        NewUser().FullName.ShouldBe("Lina Haddad");
    }

    [Fact]
    public void Register_WithSurroundingSpace_StoresTheTrimmedValues()
    {
        var user = NewUser(firstName: "  Lina  ", city: "  Amman  ");

        user.FirstName.ShouldBe("Lina");
        user.City.ShouldBe("Amman");
    }

    [Fact]
    public void Register_WithALicence_UppercasesItSoItComparesConsistently()
    {
        NewUser(licence: " jo-4471902 ").DriverLicenseNumber.ShouldBe("JO-4471902");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithoutACity_IsRefused(string city)
    {
        Should.Throw<ArgumentException>(() => NewUser(city: city))
            .Message.ShouldContain("city", Case.Insensitive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithoutAName_IsRefused(string firstName)
    {
        Should.Throw<ArgumentException>(() => NewUser(firstName: firstName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Describe_WithABlankSecondAddressLine_StoresNothingRatherThanBlank(string? addressLine2)
    {
        NewUser(addressLine2: addressLine2).AddressLine2.ShouldBeNull();
    }

    [Fact]
    public void Describe_WhenCalled_LeavesIdentityAndAuditFieldsAlone()
    {
        var user = NewUser();
        var id = user.Id;

        user.Describe("Nadia", "Haddad", "+962790000000", null, "1 Rainbow St", null, "Irbid", "Jordan", "JO-1");

        user.Id.ShouldBe(id);
        user.Email.ShouldBe("lina@example.com");
        user.City.ShouldBe("Irbid");
    }

    private static ApplicationUser NewUser(
        string firstName = "Lina",
        string city = "Amman",
        string licence = "JO-4471902",
        string? addressLine2 = null) =>
        ApplicationUser.Register(
            "lina@example.com",
            firstName,
            "Haddad",
            "+962791234567",
            new DateOnly(1996, 4, 12),
            "22 Wasfi Al-Tal St",
            addressLine2,
            city,
            "Jordan",
            licence);
}
