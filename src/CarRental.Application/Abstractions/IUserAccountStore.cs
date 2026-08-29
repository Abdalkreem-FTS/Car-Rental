using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CarRental.Application.Abstractions;

public sealed record Renter(bool EmailConfirmed, DateOnly? DateOfBirth);

public sealed record UserWithRoles(ApplicationUser User, List<string> Roles);

public interface IUserAccountStore
{
    Task<Result<IdentityResult>> CreateAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default);

    Task<Result<IdentityResult>> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task<Renter?> GetRenterAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserWithRoles?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<string?> CurrentSecurityStampAsync(Guid userId, CancellationToken cancellationToken = default);
}
