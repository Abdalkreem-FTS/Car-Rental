using CarRental.Application.Abstractions;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence;

public sealed class UserRegistrar(UserManager<ApplicationUser> userManager) : IUserRegistrar
{
    public async Task<Result<IdentityResult>> CreateAsync(
        ApplicationUser user,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await userManager.CreateAsync(user, password);
        }
        catch (DbUpdateException exception) when (DatabaseConflicts.ErrorFor(exception) is { } conflict)
        {
            return conflict;
        }
    }
}
