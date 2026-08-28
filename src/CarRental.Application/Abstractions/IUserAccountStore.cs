using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CarRental.Application.Abstractions;

public interface IUserAccountStore
{
    Task<Result<IdentityResult>> CreateAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default);

    Task<Result<IdentityResult>> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task<bool> HasConfirmedEmailAsync(Guid userId, CancellationToken cancellationToken = default);
}
