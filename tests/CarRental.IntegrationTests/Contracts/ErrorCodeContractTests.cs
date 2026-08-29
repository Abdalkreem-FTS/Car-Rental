using CarRental.Domain.Common;
using CarRental.Domain.Errors;
using Shouldly;

namespace CarRental.IntegrationTests.Contracts;

public sealed class ErrorCodeContractTests
{
    private static readonly (string PublishedCode, Error Error)[] Catalog =
    [
        ("user.email_already_in_use", UserErrors.EmailAlreadyInUse("someone@example.com")),
        ("user.license_already_in_use", UserErrors.LicenseAlreadyInUse),
        ("user.not_found", UserErrors.NotFound),
        ("user.invalid_credentials", UserErrors.InvalidCredentials),
        ("user.locked_out", UserErrors.LockedOut),
        ("auth.invalid_refresh_token", AuthErrors.InvalidRefreshToken),
        ("auth.not_authenticated", AuthErrors.NotAuthenticated),
        ("car.not_found", CarErrors.NotFound),
        ("car.plate_already_in_use", CarErrors.PlateAlreadyInUse("AMM-0001")),
        ("car.unavailable", CarErrors.Unavailable),
        ("car.has_active_bookings", CarErrors.HasActiveBookings(1)),
        ("reservation.not_found", ReservationErrors.NotFound),
        ("reservation.already_cancelled", ReservationErrors.AlreadyCancelled),
        ("reservation.already_started", ReservationErrors.AlreadyStarted),
    ];

    public static TheoryData<string, string, string> PublishedErrors
    {
        get
        {
            var rows = new TheoryData<string, string, string>();

            foreach (var (publishedCode, error) in Catalog)
            {
                rows.Add(publishedCode, error.Code, error.Description);
            }

            return rows;
        }
    }

    public static TheoryData<string, string, ErrorType> FieldKeyedErrors => new()
    {
        { "currentPassword", UserErrors.IncorrectPassword.Code, UserErrors.IncorrectPassword.Type },
        { "token", AuthErrors.InvalidResetToken.Code, AuthErrors.InvalidResetToken.Type },
    };

    [Theory]
    [MemberData(nameof(PublishedErrors))]
    public void ErrorCode_ForAPublishedFailure_KeepsTheStringClientsBranchOn(
        string publishedCode, string actualCode, string description)
    {
        actualCode.ShouldBe(publishedCode);

        description.ShouldNotBeNullOrWhiteSpace();
    }


    [Fact]
    public void PublishedCodes_AcrossTheWholeCatalog_AreUnique()
    {
        var codes = Catalog.Select(entry => entry.PublishedCode).ToList();

        codes.Distinct().Count().ShouldBe(codes.Count, "two failures sharing a code cannot be told apart");
    }
    
    [Theory]
    [MemberData(nameof(FieldKeyedErrors))]
    public void ValidationError_UsesAFieldNameRatherThanADottedCode(string field, string code, ErrorType type)
    {
        type.ShouldBe(ErrorType.Validation);
        code.ShouldBe(field);
        code.ShouldNotContain(".");
    }
}
