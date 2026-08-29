using CarRental.Application.Abstractions;
using CarRental.Domain.Common;
using CarRental.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence;

public sealed class UserAccountStore(UserManager<ApplicationUser> userManager, AppDbContext context) : IUserAccountStore
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

    public Task<Renter?> GetRenterAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new Renter(user.EmailConfirmed, user.DateOfBirth))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<UserWithRoles?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserWithRoles(
                user,
                context.UserRoles
                    .Where(link => link.UserId == user.Id)
                    .Join(context.Roles, link => link.RoleId, role => role.Id, (_, role) => role.Name!)
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

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
