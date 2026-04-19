using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentoraX.Infrastructure.Persistence.Configurations;

public sealed class LearningMaterialConfiguration : IEntityTypeConfiguration<LearningMaterial>
{
    public void Configure(EntityTypeBuilder<LearningMaterial> builder)
    {
        builder.ToTable("LearningMaterials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Tags).HasMaxLength(500);
        builder.HasOne(x => x.User).WithMany(x => x.Materials).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
