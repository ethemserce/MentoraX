using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Queries;

public sealed record GetStudyPlansQuery(Guid UserId)
    : IQuery<IReadOnlyCollection<StudyPlanDto>>;

public sealed class GetStudyPlansQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetStudyPlansQuery, IReadOnlyCollection<StudyPlanDto>>
{
    public async Task<IReadOnlyCollection<StudyPlanDto>> Handle(
        GetStudyPlansQuery query,
        CancellationToken cancellationToken)
    {
        var plans = await dbContext.StudyPlans
            .AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .Include(x => x.Items)
                .ThenInclude(x => x.MaterialChunk)
            .Include(x => x.Items)
                .ThenInclude(x => x.StudySessions)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return plans
            .Select(plan =>
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
                            s.ReviewNotes
                        ))
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
                                    i.MaterialChunk.EstimatedStudyMinutes
                                ),
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
                                    s.ReviewNotes
                                ))
                                .ToList()
                        ))
                        .ToList()
                );
            })
            .ToList();
    }
}