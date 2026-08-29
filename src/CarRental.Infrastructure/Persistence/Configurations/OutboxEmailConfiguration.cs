using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public sealed class OutboxEmailConfiguration : IEntityTypeConfiguration<OutboxEmail>
{
    public void Configure(EntityTypeBuilder<OutboxEmail> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Recipient).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Link).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(1000);

        builder.HasIndex(x => new { x.SentAtUtc, x.AbandonedAtUtc, x.NextAttemptAtUtc });
    }
}
