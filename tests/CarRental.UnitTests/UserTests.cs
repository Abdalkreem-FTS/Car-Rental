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

    private static ApplicationUser NewUser() => new()
    {
        UserName = "lina@example.com",
        Email = "lina@example.com",
        PhoneNumber = "+962791234567",
        FirstName = "Lina",
        LastName = "Haddad",
        AddressLine1 = "22 Wasfi Al-Tal St",
        City = "Amman",
        Country = "Jordan",
        DriverLicenseNumber = "JO-4471902",
    };
}
