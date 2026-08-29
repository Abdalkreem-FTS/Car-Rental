using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public sealed class IdempotentRequestConfiguration : IEntityTypeConfiguration<IdempotentRequest>
{
    public const string UniqueIndexName = "IX_IdempotentRequests_UserId_Endpoint_Key";

    public void Configure(EntityTypeBuilder<IdempotentRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Endpoint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Key).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Endpoint, x.Key })
            .IsUnique()
            .HasDatabaseName(UniqueIndexName);

        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
