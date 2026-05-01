using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
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

        var planChanges = await dbContext.StudyPlans
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => new SyncChangeDto(
                "StudyPlan",
                x.Id,
                "Upsert",
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var planItemChanges = await dbContext.StudyPlanItems
            .AsNoTracking()
            .Where(x =>
                x.StudyPlan.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => new SyncChangeDto(
                "StudyPlanItem",
                x.Id,
                "Upsert",
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var sessionChanges = await dbContext.StudySessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => new SyncChangeDto(
                "StudySession",
                x.Id,
                "Upsert",
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var materialChanges = await dbContext.LearningMaterials
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => new SyncChangeDto(
                "LearningMaterial",
                x.Id,
                "Upsert",
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var chunkChanges = await dbContext.MaterialChunks
            .AsNoTracking()
            .Where(x =>
                x.LearningMaterial.UserId == query.UserId &&
                (x.UpdatedAtUtc ?? x.CreatedAtUtc) > sinceUtc)
            .Select(x => new SyncChangeDto(
                "MaterialChunk",
                x.Id,
                "Upsert",
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var changes = planChanges
            .Concat(planItemChanges)
            .Concat(sessionChanges)
            .Concat(materialChanges)
            .Concat(chunkChanges)
            .OrderBy(x => x.ChangedAtUtc)
            .ToList();

        return new SyncChangesDto(DateTime.UtcNow, changes);
    }
}
