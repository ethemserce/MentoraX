using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentoraX.Infrastructure.Persistence.Configurations;

public sealed class StudyPlanItemConfiguration : IEntityTypeConfiguration<StudyPlanItem>
{
    public void Configure(EntityTypeBuilder<StudyPlanItem> builder)
    {
        builder.ToTable("StudyPlanItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.SourceReason)
            .HasMaxLength(500);

        builder.Property(x => x.ItemType)
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.HasIndex(x => x.StudyPlanId);
        builder.HasIndex(x => x.MaterialChunkId);
        builder.HasIndex(x => x.PlannedDateUtc);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.StudyPlan)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.StudyPlanId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.MaterialChunk)
            .WithMany(x => x.StudyPlanItems)
            .HasForeignKey(x => x.MaterialChunkId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}