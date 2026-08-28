using CarRental.Application.Abstractions;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence;

public sealed class UserAccountStore(UserManager<ApplicationUser> userManager) : IUserAccountStore
{
    public Task<Result<IdentityResult>> CreateAsync(
        ApplicationUser user,
        string password,
        CancellationToken cancellationToken = default) =>
        WithoutConflictsAsync(() => userManager.CreateAsync(user, password));

    public Task<Result<IdentityResult>> UpdateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default) =>
        WithoutConflictsAsync(() => userManager.UpdateAsync(user));

    public async Task<bool> HasConfirmedEmailAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await userManager.FindByIdAsync(userId.ToString()) is { EmailConfirmed: true };

    private static async Task<Result<IdentityResult>> WithoutConflictsAsync(Func<Task<IdentityResult>> write)
    {
        try
        {
            return await write();
        }
        catch (DbUpdateException exception) when (DatabaseConflicts.ErrorFor(exception) is { } conflict)
        {
            return conflict;
        }
    }
}
