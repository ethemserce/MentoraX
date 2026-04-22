using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Queries;

public sealed record GetStudyPlansQuery(Guid UserId) : IQuery<IReadOnlyCollection<StudyPlanDto>>;

public sealed class GetStudyPlansQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetStudyPlansQuery, IReadOnlyCollection<StudyPlanDto>>
{
    public async Task<IReadOnlyCollection<StudyPlanDto>> Handle(GetStudyPlansQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.StudyPlans.AsNoTracking().Where(x => x.UserId == query.UserId)
            .Select(x => new StudyPlanDto(x.Id, x.UserId, x.LearningMaterialId, x.Title, x.StartDate, x.DailyTargetMinutes, x.Status.ToString(),
                x.StudySessions.OrderBy(s => s.Order)
                .Select(s => new StudySessionDto(s.Id, s.StudyPlanId, s.Order, s.ScheduledAtUtc, s.StudyPlan.DailyTargetMinutes, s.StudyPlan.Status.ToString(), s.CompletedAtUtc, s.ActualDurationMinutes, s.ReviewNotes)).ToList())).ToListAsync(cancellationToken);
    }
}
