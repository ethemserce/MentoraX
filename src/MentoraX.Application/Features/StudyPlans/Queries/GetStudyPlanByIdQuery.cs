using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Queries;

public sealed record GetStudyPlanByIdQuery(Guid Id) : IQuery<StudyPlanDto?>;

public sealed class GetStudyPlanByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetStudyPlanByIdQuery, StudyPlanDto?>
{
    public async Task<StudyPlanDto?> Handle(
        GetStudyPlanByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await dbContext.StudyPlans
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new StudyPlanDto(
                x.Id,
                x.UserId,
                x.LearningMaterialId,
                x.Title,
                x.StartDate,
                x.DailyTargetMinutes,
                x.Status.ToString(),
                x.StudySessions
                    .OrderBy(s => s.Order)
                    .Select(s => new StudySessionDto(
                        s.Id,
                        s.StudyPlanId,
                        s.Order,
                        s.ScheduledAtUtc,
                        x.DailyTargetMinutes,
                        s.IsCompleted ? "Completed" : "Active",
                        s.CompletedAtUtc,
                        s.ActualDurationMinutes,
                        s.ReviewNotes,
                        s.StudyProgress != null ? s.StudyProgress.EasinessFactor : null,
                        s.StudyProgress != null ? s.StudyProgress.IntervalDays : null,
                        s.StudyProgress != null ? s.StudyProgress.RepetitionCount : null
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}