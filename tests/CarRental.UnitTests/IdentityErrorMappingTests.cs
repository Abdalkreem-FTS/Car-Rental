using CarRental.Application.Mapping;
using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace CarRental.UnitTests;

public sealed class IdentityErrorMappingTests
{
    private static readonly Error Fallback = UserErrors.RegistrationFailed;

    [Theory]
    [InlineData("PasswordTooShort")]
    [InlineData("PasswordRequiresDigit")]
    [InlineData("PasswordRequiresUpper")]
    [InlineData("PasswordRequiresLower")]
    [InlineData("PasswordRequiresNonAlphanumeric")]
    [InlineData("PasswordRequiresUniqueChars")]
    public void Map_ForAPasswordRule_IsAValidationErrorOnTheFieldTheCallerNames(string code)
    {
        var error = Map(code).ShouldHaveSingleItem();

        error.Type.ShouldBe(ErrorType.Validation);
        error.Field.ShouldBe("newPassword");
    }

    [Theory]
    [InlineData("DuplicateUserName")]
    [InlineData("DuplicateEmail")]
    public void Map_ForADuplicate_IsAConflictWhicheverCodeIdentityChose(string code)
    {
        var error = Map(code).ShouldHaveSingleItem();

        error.Type.ShouldBe(ErrorType.Conflict);
        error.Code.ShouldBe(UserErrors.EmailAlreadyInUse().Code);
    }

    [Fact]
    public void Map_ForPasswordMismatch_IsNotTreatedAsAPasswordRule()
    {
        var error = Map("PasswordMismatch").ShouldHaveSingleItem();

        error.Code.ShouldBe(UserErrors.IncorrectPassword.Code);
        error.Field.ShouldBe("currentPassword", "the field to correct is the one they typed wrong");
    }

    [Theory]
    [InlineData("ConcurrencyFailure")]
    [InlineData("UserAlreadyHasPassword")]
    [InlineData("SomeCodeIdentityAddsInTheNextRelease")]
    public void Map_ForAnythingUnrecognised_IsNotBlamedOnTheCaller(string code)
    {
        var error = Map(code).ShouldHaveSingleItem();

        error.ShouldBe(Fallback);
        error.Type.ShouldBe(ErrorType.Failure, "an unmapped Identity failure is ours until we decide otherwise");
    }

    [Fact]
    public void Map_ForSeveralErrors_KeepsEachOne()
    {
        var errors = IdentityErrors.Map(
            IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Too short." },
                new IdentityError { Code = "PasswordRequiresDigit", Description = "Needs a digit." }),
            "newPassword",
            Fallback);

        errors.Count.ShouldBe(2);
        errors.ShouldAllBe(error => error.Field == "newPassword");
    }

    [Fact]
    public void Contains_ForACodeIdentityRaised_FindsIt()
    {
        var result = IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Bad token." });

        result.Contains(IdentityErrors.InvalidToken).ShouldBeTrue();
        result.Contains(IdentityErrors.PasswordMismatch).ShouldBeFalse();
    }

    private static List<Error> Map(string code) =>
        IdentityErrors.Map(
            IdentityResult.Failed(new IdentityError { Code = code, Description = "Identity said no." }),
            "newPassword",
            Fallback);
}
