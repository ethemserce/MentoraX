using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Mobile.Queries;

public sealed record GetMobileDashboardQuery(Guid UserId) : IQuery<MobileDashboardDto>;

public sealed class GetMobileDashboardQueryHandler(IApplicationDbContext _dbContext)
    : IQueryHandler<GetMobileDashboardQuery, MobileDashboardDto>
{
    public async Task<MobileDashboardDto> Handle(
        GetMobileDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var activePendingSessionsQuery = _dbContext.StudySessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                !x.IsCompleted &&
                x.StudyPlan != null &&
                x.StudyPlan.Status == PlanStatus.Active);

        var dueCount = await activePendingSessionsQuery
            .CountAsync(
                x => x.ScheduledAtUtc <= now,
                cancellationToken);

        var todayPlannedMinutes = await activePendingSessionsQuery
            .Where(x => DateOnly.FromDateTime(x.ScheduledAtUtc) == today)
            .SumAsync(
                x => x.StudyPlan != null
                    ? x.StudyPlan.DailyTargetMinutes
                    : 0,
                cancellationToken);

        var todayCompletedMinutes = await _dbContext.StudySessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                x.IsCompleted &&
                x.CompletedAtUtc.HasValue &&
                DateOnly.FromDateTime(x.CompletedAtUtc.Value) == today)
            .SumAsync(
                x => x.ActualDurationMinutes ?? 0,
                cancellationToken);

        var nextSession = await activePendingSessionsQuery
            .OrderBy(x => x.ScheduledAtUtc)
            .Select(x => new NextStudySessionDto(
                x.Id,
                x.StudyPlanId,
                x.LearningMaterialId,
                x.LearningMaterial != null
                    ? x.LearningMaterial.Title
                    : "Untitled Material",
                x.ScheduledAtUtc,
                x.StartedAtUtc,
                x.StudyPlan != null
                    ? x.StudyPlan.DailyTargetMinutes
                    : 0,
                x.ScheduledAtUtc <= now
            ))
            .FirstOrDefaultAsync(cancellationToken);

        var weakMaterialsRaw = await _dbContext.StudyProgresses
            .AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .OrderBy(x => x.EasinessFactor)
            .ThenBy(x => x.NextReviewAtUtc)
            .Join(
                _dbContext.LearningMaterials.AsNoTracking(),
                progress => progress.LearningMaterialId,
                material => material.Id,
                (progress, material) => new
                {
                    material.Id,
                    material.Title,
                    progress.EasinessFactor,
                    progress.NextReviewAtUtc
                })
            .Take(20)
            .ToListAsync(cancellationToken);

        var weakMaterials = weakMaterialsRaw
            .Select(x => new WeakMaterialDto(
                x.Id,
                x.Title,
                x.EasinessFactor >= 2.5
                    ? "Strong"
                    : x.EasinessFactor >= 2.0
                        ? "Medium"
                        : "Weak",
                x.NextReviewAtUtc))
            .Where(x => x.PerformanceLevel == "Weak")
            .Take(5)
            .ToList();

        return new MobileDashboardDto(
            dueCount,
            todayPlannedMinutes,
            todayCompletedMinutes,
            nextSession,
            weakMaterials);
    }
}