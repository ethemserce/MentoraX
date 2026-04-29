using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudySessions.Queries;

public sealed record GetDueStudySessionsQuery(
    Guid UserId,
    DateTime? UntilUtc
) : IQuery<IReadOnlyCollection<StudySessionDto>>;

public sealed class GetDueStudySessionsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetDueStudySessionsQuery, IReadOnlyCollection<StudySessionDto>>
{
    public async Task<IReadOnlyCollection<StudySessionDto>> Handle(
        GetDueStudySessionsQuery query,
        CancellationToken cancellationToken)
    {
        var until = query.UntilUtc ?? DateTime.UtcNow;

        return await dbContext.StudySessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                !x.IsCompleted &&
                x.StudyPlan != null &&
                x.StudyPlan.Status == PlanStatus.Active &&
                x.ScheduledAtUtc <= until)
            .OrderBy(x => x.ScheduledAtUtc)
            .Select(x => new StudySessionDto(
                x.Id,
                x.StudyPlanId,
                x.Order,
                x.ScheduledAtUtc,
                x.StudyPlan!.DailyTargetMinutes,
                "Planned",
                x.CompletedAtUtc,
                x.ActualDurationMinutes,
                x.ReviewNotes
            ))
            .ToListAsync(cancellationToken);
    }
}