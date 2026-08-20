using Microsoft.AspNetCore.Identity;

namespace CarRental.Domain.Entities;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }
}

public static class Roles
{
    public const string Customer = nameof(Customer);
    public const string Admin = nameof(Admin);

    public static readonly string[] All = [Customer, Admin];
}
