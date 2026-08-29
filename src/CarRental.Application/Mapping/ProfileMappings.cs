using CarRental.Application.Contracts.Profile;
using CarRental.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace CarRental.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class ProfileMappings
{
    [MapProperty(
        nameof(UpdateProfileRequest.DriverLicenseNumber),
        nameof(ApplicationUser.DriverLicenseNumber),
        Use = nameof(NormaliseLicence))]
    public static partial void ApplyTo(this UpdateProfileRequest request, ApplicationUser user);

    [UserMapping(Default = false)]
    public static string NormaliseLicence(string value) => value.Trim().ToUpperInvariant();

    [UserMapping(Default = true)]
    private static string Tidy(string value) => value.Trim();

    private static string? TidyOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
