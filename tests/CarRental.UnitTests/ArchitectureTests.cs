using System.Text.RegularExpressions;
using Shouldly;

namespace CarRental.UnitTests;

public sealed class ArchitectureTests
{
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        ["CarRental.Domain"] = [],
        ["CarRental.Application"] = ["CarRental.Domain"],
        ["CarRental.Infrastructure"] = ["CarRental.Application"],
        ["CarRental.Api"] = ["CarRental.Application", "CarRental.Infrastructure"],
    };

    [Theory]
    [InlineData("CarRental.Domain")]
    [InlineData("CarRental.Application")]
    [InlineData("CarRental.Infrastructure")]
    [InlineData("CarRental.Api")]
    public void EveryProject_ReferencesOnlyWhatTheReadmeSaysItDoes(string project)
    {
        var referenced = ProjectReferencesOf(project);

        referenced.ShouldBe(
            Allowed[project].Order().ToList(),
            $"the Architecture section of README.md describes {project}'s references. "
            + "A reader who checks one claim and finds it false stops trusting the rest.");
    }

    [Fact]
    public void Domain_TakesNoPackageThatWouldPullInfrastructureIntoIt()
    {
        var packages = PackageReferencesOf("CarRental.Domain");

        packages.ShouldBe(
            ["Microsoft.Extensions.Identity.Stores"],
            "Identity is the one documented exception. Anything else here means the domain "
            + "has grown a dependency on a database, a transport or a clock.");
    }

    private static List<string> ProjectReferencesOf(string project) =>
        [.. Matches(project, @"<ProjectReference\s+Include=""[^""]*[\\/]([^\\/""]+)\.csproj""")];

    private static List<string> PackageReferencesOf(string project) =>
        [.. Matches(project, @"<PackageReference\s+Include=""([^""]+)""")];

    private static IEnumerable<string> Matches(string project, string pattern) =>
        Regex.Matches(File.ReadAllText(ProjectFile(project)), pattern)
            .Select(match => match.Groups[1].Value)
            .Order();

    private static string ProjectFile(string project)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new InvalidOperationException("Could not find the repository root.");

        return Path.Combine(root, "src", project, $"{project}.csproj");
    }
}
