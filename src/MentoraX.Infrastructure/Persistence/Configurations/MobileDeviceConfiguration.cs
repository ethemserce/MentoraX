using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentoraX.Infrastructure.Persistence.Configurations;

public sealed class MobileDeviceConfiguration : IEntityTypeConfiguration<MobileDevice>
{
    public void Configure(EntityTypeBuilder<MobileDevice> builder)
    {
        builder.ToTable("MobileDevices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeviceToken)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Platform)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.MobileDevices)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.DeviceToken })
            .IsUnique();
    }
}