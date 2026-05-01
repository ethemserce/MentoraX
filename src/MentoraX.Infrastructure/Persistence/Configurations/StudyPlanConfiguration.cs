using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentoraX.Infrastructure.Persistence.Configurations;

public sealed class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.ToTable("StudyPlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.HasOne(x => x.User).WithMany(x => x.StudyPlans).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LearningMaterial).WithMany(x=>x.StudyPlans).HasForeignKey(x => x.LearningMaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.StudySessions).WithOne(x => x.StudyPlan).HasForeignKey(x => x.StudyPlanId).OnDelete(DeleteBehavior.Cascade);
    }
}
