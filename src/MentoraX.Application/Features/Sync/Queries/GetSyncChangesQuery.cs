using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Sync.Queries;

public sealed record GetSyncChangesQuery(Guid UserId, DateTime SinceUtc)
    : IQuery<SyncChangesDto>;

public sealed class GetSyncChangesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetSyncChangesQuery, SyncChangesDto>
{
    public async Task<SyncChangesDto> Handle(
        GetSyncChangesQuery query,
        CancellationToken cancellationToken)
    {
        var sinceUtc = DateTime.SpecifyKind(query.SinceUtc, DateTimeKind.Utc);

        var changedPlanIds = await dbContext.StudyPlans
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var changedPlanIdsFromItems = await dbContext.StudyPlanItems
            .AsNoTracking()
            .Where(x =>
                x.StudyPlan.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => x.StudyPlanId)
            .ToListAsync(cancellationToken);

        var changedPlanIdsFromSessions = await dbContext.StudySessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => x.StudyPlanId)
            .ToListAsync(cancellationToken);

        var changedMaterialIds = await dbContext.LearningMaterials
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var changedChunkMaterialIds = await dbContext.MaterialChunks
            .AsNoTracking()
            .Where(x =>
                x.LearningMaterial.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => x.LearningMaterialId)
            .ToListAsync(cancellationToken);

        var materialPlanIds = await dbContext.StudyPlans
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                (changedMaterialIds.Contains(x.LearningMaterialId) ||
                 changedChunkMaterialIds.Contains(x.LearningMaterialId)))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var affectedPlanIds = changedPlanIds
            .Concat(changedPlanIdsFromItems)
            .Concat(changedPlanIdsFromSessions)
            .Concat(materialPlanIds)
            .Distinct()
            .ToList();

        if (affectedPlanIds.Count == 0)
        {
            return new SyncChangesDto(DateTime.UtcNow, Array.Empty<SyncChangeDto>());
        }

        var plans = await dbContext.StudyPlans
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                affectedPlanIds.Contains(x.Id))
            .Include(x => x.Items)
                .ThenInclude(x => x.MaterialChunk)
            .Include(x => x.Items)
                .ThenInclude(x => x.StudySessions)
            .ToListAsync(cancellationToken);

        var changes = plans
            .Select(plan =>
            {
                var changedAtUtc = Max(
                    plan.UpdatedAtUtc ?? plan.CreatedAtUtc,
                    plan.Items.Select(x => x.UpdatedAtUtc ?? x.CreatedAtUtc),
                    plan.Items.SelectMany(x => x.StudySessions).Select(x => x.UpdatedAtUtc ?? x.CreatedAtUtc),
                    plan.Items
                        .Where(x => x.MaterialChunk is not null)
                        .Select(x => x.MaterialChunk!.UpdatedAtUtc ?? x.MaterialChunk.CreatedAtUtc));

                return new SyncChangeDto(
                    "StudyPlan",
                    plan.Id,
                    "Upsert",
                    changedAtUtc,
                    ToStudyPlanDto(plan));
            })
            .OrderBy(x => x.ChangedAtUtc)
            .ToList();

        return new SyncChangesDto(DateTime.UtcNow, changes);
    }

    private static StudyPlanDto ToStudyPlanDto(StudyPlan plan)
    {
        var sessions = plan.Items
            .SelectMany(i => i.StudySessions)
            .OrderBy(s => s.Order)
            .ToList();

        return new StudyPlanDto(
            plan.Id,
            plan.UserId,
            plan.LearningMaterialId,
            plan.Title,
            plan.StartDate,
            plan.DailyTargetMinutes,
            plan.Status.ToString(),
            sessions
                .Select(s => new StudySessionDto(
                    s.Id,
                    s.StudyPlanId,
                    s.Order,
                    s.ScheduledAtUtc,
                    plan.DailyTargetMinutes,
                    s.IsCompleted ? "Completed" : "Planned",
                    s.CompletedAtUtc,
                    s.ActualDurationMinutes,
                    s.ReviewNotes))
                .ToList(),
            plan.Items
                .OrderBy(i => i.OrderNo)
                .Select(i => new StudyPlanItemDto(
                    i.Id,
                    i.StudyPlanId,
                    i.MaterialChunkId,
                    i.Title,
                    i.Description,
                    i.ItemType.ToString(),
                    i.OrderNo,
                    i.PlannedDateUtc,
                    i.PlannedStartTime,
                    i.PlannedEndTime,
                    i.DurationMinutes,
                    i.Status.ToString(),
                    i.MaterialChunk is null
                        ? null
                        : new MaterialChunkDto(
                            i.MaterialChunk.Id,
                            i.MaterialChunk.LearningMaterialId,
                            i.MaterialChunk.OrderNo,
                            i.MaterialChunk.Title,
                            i.MaterialChunk.Content,
                            i.MaterialChunk.Summary,
                            i.MaterialChunk.Keywords,
                            i.MaterialChunk.DifficultyLevel,
                            i.MaterialChunk.EstimatedStudyMinutes,
                            i.MaterialChunk.CharacterCount,
                            i.MaterialChunk.IsGeneratedByAI),
                    i.StudySessions
                        .OrderBy(s => s.Order)
                        .Select(s => new StudySessionDto(
                            s.Id,
                            s.StudyPlanId,
                            s.Order,
                            s.ScheduledAtUtc,
                            plan.DailyTargetMinutes,
                            s.IsCompleted ? "Completed" : "Planned",
                            s.CompletedAtUtc,
                            s.ActualDurationMinutes,
                            s.ReviewNotes))
                        .ToList()))
                .ToList());
    }

    private static DateTime Max(DateTime first, params IEnumerable<DateTime>[] values)
    {
        var max = first;

        foreach (var value in values.SelectMany(x => x))
        {
            if (value > max)
            {
                max = value;
            }
        }

        return max;
    }
}
