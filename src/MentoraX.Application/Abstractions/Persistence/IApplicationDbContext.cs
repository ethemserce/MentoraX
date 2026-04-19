using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<LearningMaterial> LearningMaterials { get; }
    DbSet<StudyPlan> StudyPlans { get; }
    DbSet<StudySession> StudySessions { get; }
    DbSet<StudyProgress> StudyProgresses { get; }
    DbSet<MobileDevice> MobileDevices { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
