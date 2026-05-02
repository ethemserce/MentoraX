using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using MentoraX.Domain.Enums;
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

        var changedMaterials = await dbContext.LearningMaterials
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Include(x => x.StudyPlans)
            .ToListAsync(cancellationToken);

        var changedChunks = await dbContext.MaterialChunks
            .AsNoTracking()
            .Where(x =>
                x.LearningMaterial.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .ToListAsync(cancellationToken);

        var changedSessions = await dbContext.StudySessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Include(x => x.StudyPlan)
            .Include(x => x.LearningMaterial)
            .Include(x => x.StudyPlanItem!)
                .ThenInclude(x => x.MaterialChunk)
            .ToListAsync(cancellationToken);

        var planChanges = plans
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
            .ToList();

        var materialChanges = changedMaterials
            .Select(material => new SyncChangeDto(
                "Material",
                material.Id,
                "Upsert",
                ChangedAt(material),
                ToMaterialDto(material)))
            .ToList();

        var chunkChanges = changedChunks
            .Select(chunk => new SyncChangeDto(
                "MaterialChunk",
                chunk.Id,
                "Upsert",
                ChangedAt(chunk),
                ToMaterialChunkDto(chunk)))
            .ToList();

        var sessionChanges = changedSessions
            .Select(session => new SyncChangeDto(
                "StudySession",
                session.Id,
                "Upsert",
                ChangedAt(session),
                ToStudySessionDetailDto(session)))
            .ToList();

        var changes = materialChanges
            .Concat(chunkChanges)
            .Concat(sessionChanges)
            .Concat(planChanges)
            .OrderBy(x => x.ChangedAtUtc)
            .ToList();

        return new SyncChangesDto(DateTime.UtcNow, changes);
    }

    private static MaterialDto ToMaterialDto(LearningMaterial material)
    {
        var activePlan = material.StudyPlans
            .Where(x => x.Status == PlanStatus.Active || x.Status == PlanStatus.Paused)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

        return new MaterialDto(
            material.Id,
            material.UserId,
            material.Title,
            material.MaterialType.ToString(),
            material.Content,
            material.EstimatedDurationMinutes,
            material.Description,
            material.Tags,
            activePlan is not null,
            activePlan?.Id,
            activePlan?.Title);
    }

    private static MaterialChunkDto ToMaterialChunkDto(MaterialChunk chunk)
    {
        return new MaterialChunkDto(
            chunk.Id,
            chunk.LearningMaterialId,
            chunk.OrderNo,
            chunk.Title,
            chunk.Content,
            chunk.Summary,
            chunk.Keywords,
            chunk.DifficultyLevel,
            chunk.EstimatedStudyMinutes,
            chunk.CharacterCount,
            chunk.IsGeneratedByAI);
    }

    private static StudySessionDetailDto ToStudySessionDetailDto(StudySession session)
    {
        var item = session.StudyPlanItem;
        var chunk = item?.MaterialChunk;

        return new StudySessionDetailDto(
            session.Id,
            session.StudyPlanId,
            session.StudyPlanItemId,
            session.LearningMaterialId,
            chunk?.Id,
            session.StudyPlan.Title,
            session.LearningMaterial.Title,
            chunk?.Title,
            chunk?.Content,
            item?.ItemType.ToString(),
            session.Order,
            session.ScheduledAtUtc,
            session.StartedAtUtc,
            session.StudyPlan.DailyTargetMinutes,
            ToSessionStatus(session),
            session.CompletedAtUtc,
            session.ActualDurationMinutes,
            session.ReviewNotes);
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
                    ToSessionStatus(s),
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
                            ToSessionStatus(s),
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

    private static DateTime ChangedAt(BaseEntity entity)
    {
        return entity.UpdatedAtUtc ?? entity.CreatedAtUtc;
    }

    private static string ToSessionStatus(StudySession session)
    {
        if (session.IsCompleted)
        {
            return "Completed";
        }

        return session.StartedAtUtc.HasValue ? "InProgress" : "Planned";
    }
}
