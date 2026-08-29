using CarRental.Domain.Entities;
using CarRental.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Car> Cars => Set<Car>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<IdempotentRequest> IdempotentRequests => Set<IdempotentRequest>();

    public DbSet<OutboxEmail> OutboxEmails => Set<OutboxEmail>();
    
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);

        builder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(DependencyInjection).Assembly);
    }
}
