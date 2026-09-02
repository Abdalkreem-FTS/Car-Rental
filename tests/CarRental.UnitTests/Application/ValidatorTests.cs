using CarRental.Application.Dtos.Auth;
using CarRental.Application.Validators;
using CarRental.Application.Validators.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Shouldly;

namespace CarRental.UnitTests.Application;

public sealed class ValidatorTests
{
    private static readonly IdentityOptions Identity = new()
    {
        Password =
        {
            RequiredLength = 8,
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = true,
        },
    };

    private readonly RegisterDtoValidator _validator = new(Options.Create(Identity));

    [Theory]
    [InlineData("Str0ng#Pass1")]
    [InlineData("aA1!aaaa")]
    [InlineData("Very#L0ngPasswordThatIsFine")]
    public void Validate_WithAPasswordMeetingEveryRule_ReportsNoPasswordError(string password)
    {
        ErrorsFor(password: password, confirmPassword: password).ShouldNotContain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("aA1!aaa", "at least 8 characters")]
    [InlineData("str0ng#pass1", "uppercase letter")]
    [InlineData("STR0NG#PASS1", "lowercase letter")]
    [InlineData("Strong#Pass", "one digit")]
    [InlineData("Str0ngPass1", "special character")]
    public void Validate_WithAPasswordMissingARule_ReportsWhichRuleFailed(string password, string expected)
    {
        var messages = ErrorsFor(password: password, confirmPassword: password)
            .Where(e => e.PropertyName == "Password")
            .Select(e => e.ErrorMessage)
            .ToList();

        messages.ShouldContain(message => message.Contains(expected));
    }

    [Fact]
    public void Validate_WithAMismatchedConfirmation_ReportsConfirmPassword()
    {
        ErrorsFor(password: "Str0ng#Pass1", confirmPassword: "Str0ng#Pass2")
            .ShouldContain(e => e.PropertyName == "ConfirmPassword");
    }

    [Fact]
    public void Validate_WithoutADateOfBirth_ReportsNoDateOfBirthError()
    {
        ErrorsFor(dateOfBirth: null).ShouldNotContain(e => e.PropertyName == "DateOfBirth");
    }

    [Fact]
    public void Validate_ForSomeoneTurningEighteenToday_ReportsNoDateOfBirthError()
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        ErrorsFor(dateOfBirth: today.AddYears(-SharedRules.MinimumRenterAge))
            .ShouldNotContain(e => e.PropertyName == "DateOfBirth");
    }

    [Fact]
    public void Validate_ForSomeoneADayShortOfEighteen_ReportsDateOfBirth()
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        ErrorsFor(dateOfBirth: today.AddYears(-SharedRules.MinimumRenterAge).AddDays(1))
            .ShouldContain(e => e.PropertyName == "DateOfBirth");
    }

    [Theory]
    [InlineData("+962791234567")]
    [InlineData("+1 (555) 123-4567")]
    [InlineData("+962 (7) 9000-0000")]
    [InlineData("  +962-79-123-4567  ")]
    public void Validate_WithAnInternationalPhoneFormat_ReportsNoPhoneNumberError(string phoneNumber)
    {
        ErrorsFor(phoneNumber: phoneNumber).ShouldNotContain(e => e.PropertyName == "PhoneNumber");
    }

    [Theory]
    [InlineData("0791234567")]
    [InlineData("+((((((((")]
    [InlineData("+")]
    [InlineData("+12")]
    [InlineData("+1234567890123456")]
    [InlineData("+962 79 abc 4567")]
    public void Validate_WithANumberWeCouldNotDial_ReportsAPhoneNumberError(string phoneNumber)
    {
        ErrorsFor(phoneNumber: phoneNumber).ShouldContain(e => e.PropertyName == "PhoneNumber");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("")]
    public void Validate_WithAnUnusablePhoneNumber_ReportsPhoneNumber(string phoneNumber)
    {
        ErrorsFor(phoneNumber: phoneNumber).ShouldContain(e => e.PropertyName == "PhoneNumber");
    }

    private List<FluentValidation.Results.ValidationFailure> ErrorsFor(
        string password = "Str0ng#Pass1",
        string? confirmPassword = null,
        string phoneNumber = "+962791234567",
        DateOnly? dateOfBirth = null) =>
        _validator.Validate(new RegisterDto(
            FirstName: "Lina",
            LastName: "Haddad",
            Email: "lina@example.com",
            Password: password,
            ConfirmPassword: confirmPassword ?? password,
            PhoneNumber: phoneNumber,
            DateOfBirth: dateOfBirth,
            AddressLine1: "22 Wasfi Al-Tal St",
            AddressLine2: null,
            City: "Amman",
            Country: "Jordan",
            DriverLicenseNumber: "JO-4471902")).Errors;
}
