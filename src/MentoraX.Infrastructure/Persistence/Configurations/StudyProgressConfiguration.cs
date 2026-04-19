using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentoraX.Infrastructure.Persistence.Configurations;

public sealed class StudyProgressConfiguration : IEntityTypeConfiguration<StudyProgress>
{
    public void Configure(EntityTypeBuilder<StudyProgress> builder)
    {
        builder.ToTable("StudyProgresses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EasinessFactor)
            .HasPrecision(5, 2);

        builder.Property(x => x.NextReviewAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.StudyProgresses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LearningMaterial)
            .WithMany(x => x.StudyProgresses)
            .HasForeignKey(x => x.LearningMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StudyPlan)
            .WithMany(x => x.StudyProgresses)
            .HasForeignKey(x => x.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StudySessions)
            .WithOne(x => x.StudyProgress)
            .HasForeignKey(x => x.StudyProgressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.LearningMaterialId, x.StudyPlanId })
            .IsUnique();
    }
}