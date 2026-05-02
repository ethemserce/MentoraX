using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentoraX.Infrastructure.Persistence.Configurations;

public sealed class SyncTombstoneConfiguration : IEntityTypeConfiguration<SyncTombstone>
{
    public void Configure(EntityTypeBuilder<SyncTombstone> builder)
    {
        builder.ToTable("SyncTombstones");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.DeletedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.DeletedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.EntityType, x.EntityId });
    }
}
