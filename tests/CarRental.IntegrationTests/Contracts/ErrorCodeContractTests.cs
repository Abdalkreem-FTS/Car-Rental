using CarRental.Domain.Common;
using CarRental.Api.Extensions;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace CarRental.IntegrationTests.Contracts;

public sealed class ErrorCodeContractTests
{
    private static readonly (string PublishedCode, Error Error)[] Catalog =
    [
        ("user.email_already_in_use", UserErrors.EmailAlreadyInUse("someone@example.com")),
        ("user.license_already_in_use", UserErrors.LicenseAlreadyInUse),
        ("user.not_found", UserErrors.NotFound),
        ("user.registration_failed", UserErrors.RegistrationFailed),
        ("user.update_failed", UserErrors.UpdateFailed),
        ("user.registration_incomplete", UserErrors.RegistrationIncomplete),
        ("user.email_not_confirmed", UserErrors.EmailNotConfirmed),
        ("user.date_of_birth_missing", UserErrors.DateOfBirthMissing),
        ("user.too_young_to_rent", UserErrors.TooYoungToRent(18)),
        ("user.invalid_credentials", UserErrors.InvalidCredentials),
        ("user.locked_out", UserErrors.LockedOut),
        ("auth.invalid_refresh_token", AuthErrors.InvalidRefreshToken),
        ("auth.refresh_token_reused", AuthErrors.RefreshTokenReused),
        ("auth.not_authenticated", AuthErrors.NotAuthenticated),
        ("request.malformed", RequestErrors.Malformed),
        ("request.no_such_endpoint", RequestErrors.NoSuchEndpoint),
        ("request.method_not_allowed", RequestErrors.MethodNotAllowed),
        ("request.not_acceptable", RequestErrors.NotAcceptable),
        ("request.unsupported_media_type", RequestErrors.UnsupportedMediaType),
        ("server.unexpected", RequestErrors.Unexpected),
        ("car.not_found", CarErrors.NotFound),
        ("car.plate_already_in_use", CarErrors.PlateAlreadyInUse("AMM-0001")),
        ("car.unavailable", CarErrors.Unavailable),
        ("car.has_active_bookings", CarErrors.HasActiveBookings(1)),
        ("reservation.not_found", ReservationErrors.NotFound),
        ("reservation.already_cancelled", ReservationErrors.AlreadyCancelled),
        ("reservation.already_started", ReservationErrors.AlreadyStarted),
        ("reservation.version_required", ReservationErrors.VersionRequired),
        ("reservation.version_stale", ReservationErrors.VersionStale),
        ("idempotency.key_reused", IdempotencyErrors.KeyReused),
        ("idempotency.in_progress", IdempotencyErrors.InProgress),
        ("idempotency.key_required", IdempotencyErrors.KeyRequired("Idempotency-Key", 128)),
        ("auth.invalid_reset_token", AuthErrors.InvalidResetToken),
        ("auth.invalid_confirmation_token", AuthErrors.InvalidConfirmationToken),
        ("user.incorrect_password", UserErrors.IncorrectPassword),
        ("car.invalid_filters", CarErrors.InvalidFilters("because")),
        ("car.invalid_sorts", CarErrors.InvalidSorts("because")),
        ("car.pickup_location_not_offered", CarErrors.PickupLocationNotOffered("Amman", "Aqaba")),
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

    public static TheoryData<Error, string> ValidationErrors => new()
    {
        { UserErrors.IncorrectPassword, "currentPassword" },
        { AuthErrors.InvalidResetToken, "token" },
        { AuthErrors.InvalidConfirmationToken, "token" },
        { CarErrors.InvalidFilters("because"), "filters" },
        { CarErrors.InvalidSorts("because"), "sorts" },
        { Error.Validation("newPassword", "too short"), "newPassword" },
    };

    [Theory]
    [MemberData(nameof(PublishedErrors))]
    public void ErrorCode_ForAPublishedFailure_KeepsTheStringClientsBranchOn(
        string publishedCode, string actualCode, string description)
    {
        actualCode.ShouldBe(publishedCode);

        description.ShouldNotBeNullOrWhiteSpace();
    }


    public static TheoryData<ErrorType, int> StatusForType => new()
    {
        { ErrorType.Validation, 400 },
        { ErrorType.BadRequest, 400 },
        { ErrorType.Unauthorized, 401 },
        { ErrorType.Forbidden, 403 },
        { ErrorType.NotFound, 404 },
        { ErrorType.Conflict, 409 },
        { ErrorType.PreconditionFailed, 412 },
        { ErrorType.PreconditionRequired, 428 },
        { ErrorType.Failure, 500 },
        { ErrorType.Unexpected, 500 },
    };

    [Theory]
    [MemberData(nameof(StatusForType))]
    public void ErrorType_MapsToTheStatusItClaims(ErrorType type, int expectedStatus)
    {
        var problem = Error.Create((int)type, "some.code", "Something went wrong.").ToProblem();

        problem.ShouldBeAssignableTo<IStatusCodeHttpResult>()!.StatusCode.ShouldBe(expectedStatus);
    }

    [Fact]
    public void UnclassifiedFailure_IsNotBlamedOnTheCaller()
    {
        Error.Failure("user.registration_failed", "Identity refused.")
            .ToProblem()
            .ShouldBeAssignableTo<IStatusCodeHttpResult>()!
            .StatusCode
            .ShouldBe(500, "a failure we cannot classify is ours until proven otherwise");
    }

    [Fact]
    public void ToProblem_WithNoErrors_FailsLoudlyRatherThanInventingAProblem()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new List<Error>().ToProblem());
    }

    [Fact]
    public void PublishedCodes_AcrossTheWholeCatalog_AreUnique()
    {
        var codes = Catalog.Select(entry => entry.PublishedCode).ToList();

        codes.Distinct().Count().ShouldBe(codes.Count, "two failures sharing a code cannot be told apart");
    }
    
    [Theory]
    [MemberData(nameof(ValidationErrors))]
    public void ValidationError_KeepsTheFieldSeparateFromTheCode(Error error, string field)
    {
        error.Type.ShouldBe(ErrorType.Validation);
        error.Field.ShouldBe(field, "the field names the input the client must correct");
        error.Field.ShouldNotContain(".", Case.Sensitive);
        error.Code.ShouldContain(".", Case.Sensitive, "the code stays a dotted domain code, whatever the type");
    }

    [Fact]
    public void Error_ForAnythingButValidation_NamesNoField()
    {
        foreach (var (_, error) in Catalog.Where(entry => entry.Error.Type != ErrorType.Validation))
        {
            error.Field.ShouldBeNull($"{error.Code} is not about a field the client can correct");
        }
    }
}
