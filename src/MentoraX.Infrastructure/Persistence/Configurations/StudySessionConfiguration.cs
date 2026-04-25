using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentoraX.Infrastructure.Persistence.Configurations;

public sealed class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.ToTable("StudySessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReviewNotes)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.StudySessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LearningMaterial)
            .WithMany(x => x.StudySessions)
            .HasForeignKey(x => x.LearningMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StudyPlan)
            .WithMany(x => x.StudySessions)
            .HasForeignKey(x => x.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.StudyProgress)
            .WithMany(x => x.StudySessions)
            .HasForeignKey(x => x.StudyProgressId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.StudyPlanItemId);

        builder.HasOne(x => x.StudyPlanItem)
            .WithMany(x => x.StudySessions)
            .HasForeignKey(x => x.StudyPlanItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.UserId, x.ScheduledAtUtc, x.IsCompleted });
    }
}