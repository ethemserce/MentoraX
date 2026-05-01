using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Infrastructure.Persistence;

public sealed class MentoraXDbContext : DbContext, IApplicationDbContext
{
    public MentoraXDbContext(DbContextOptions<MentoraXDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<LearningMaterial> LearningMaterials => Set<LearningMaterial>();
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<StudyProgress> StudyProgresses => Set<StudyProgress>();
    public DbSet<MobileDevice> MobileDevices => Set<MobileDevice>();
    public DbSet<MaterialChunk> MaterialChunks => Set<MaterialChunk>();
    public DbSet<StudyPlanItem> StudyPlanItems => Set<StudyPlanItem>();
    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MentoraXDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
