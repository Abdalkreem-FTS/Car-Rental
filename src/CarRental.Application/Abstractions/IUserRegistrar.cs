using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CarRental.Application.Abstractions;

public interface IUserRegistrar
{
    Task<Result<IdentityResult>> CreateAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default);
}
