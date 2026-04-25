using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentoraX.Infrastructure.Persistence.Configurations;

public sealed class MaterialChunkConfiguration : IEntityTypeConfiguration<MaterialChunk>
{
    public void Configure(EntityTypeBuilder<MaterialChunk> builder)
    {
        builder.ToTable("MaterialChunks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Summary)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Keywords)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.LearningMaterialId);

        builder.HasIndex(x => new { x.LearningMaterialId, x.OrderNo })
            .IsUnique();

        builder.HasOne(x => x.LearningMaterial)
            .WithMany(x => x.MaterialChunks)
            .HasForeignKey(x => x.LearningMaterialId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}