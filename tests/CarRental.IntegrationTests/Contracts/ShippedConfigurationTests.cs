using System.Text.Json;
using CarRental.Api.Extensions;
using CarRental.Domain.Common;
using Shouldly;

namespace CarRental.IntegrationTests.Contracts;

public sealed class ShippedConfigurationTests
{
    [Theory]
    [InlineData("Jwt", "Key")]
    [InlineData("Seed", "AdminPassword")]
    public void Appsettings_ShipsNoSecret(string section, string key)
    {
        foreach (var file in Directory.GetFiles(ApiProjectDirectory(), "appsettings*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));

            var present = document.RootElement.TryGetProperty(section, out var contents)
                          && contents.TryGetProperty(key, out _);

            present.ShouldBeFalse(
                $"{Path.GetFileName(file)} carries {section}:{key}. A secret in source is a secret "
                + "everyone with the repository has, placeholder or not.");
        }
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.BadRequest, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.PreconditionFailed, 412)]
    [InlineData(ErrorType.PreconditionRequired, 428)]
    [InlineData(ErrorType.Failure, 500)]
    [InlineData(ErrorType.Unexpected, 500)]
    [InlineData(ErrorType.Unavailable, 503)]
    [InlineData(ErrorType.Timeout, 504)]
    public void EveryErrorType_MapsToTheStatusTheClientSwitchesOn(ErrorType type, int expected) =>
        ProblemExtensions.StatusFor(type).ShouldBe(expected);

    [Fact]
    public void EveryErrorType_IsPinnedByATestRatherThanFallingThroughTo500()
    {
        var pinned = typeof(ShippedConfigurationTests)
            .GetMethod(nameof(EveryErrorType_MapsToTheStatusTheClientSwitchesOn))!
            .GetCustomAttributes(typeof(InlineDataAttribute), false)
            .Cast<InlineDataAttribute>()
            .Select(data => (ErrorType)data.GetData(null!).Single()[0]!)
            .ToHashSet();

        Enum.GetValues<ErrorType>().ShouldAllBe(
            type => pinned.Contains(type),
            "a new ErrorType silently falls through to 500 until someone pins the status it should carry");
    }

    private static string ApiProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "CarRental.Api")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new InvalidOperationException("Could not find the repository root."),
            "src",
            "CarRental.Api");
    }
}
