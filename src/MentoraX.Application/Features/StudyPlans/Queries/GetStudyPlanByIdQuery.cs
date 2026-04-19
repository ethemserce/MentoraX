using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Queries;

public sealed record GetStudyPlanByIdQuery(Guid StudyPlanId) : IQuery<StudyPlanDto?>;

public sealed class GetStudyPlanByIdQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetStudyPlanByIdQuery, StudyPlanDto?>
{
    public async Task<StudyPlanDto?> Handle(GetStudyPlanByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.StudyPlans.AsNoTracking().Where(x => x.Id == query.StudyPlanId)
            .Select(x => new StudyPlanDto(x.Id, x.UserId, x.LearningMaterialId, x.Title, x.StartDate, x.DailyTargetMinutes, x.Status.ToString(),
                x.StudySessions.OrderBy(s => s.Order).Select(s => new StudySessionDto(s.Id, s.StudyPlanId, s.Order, s.ScheduledAtUtc, s.StudyPlan.DailyTargetMinutes, s.StudyPlan.Status.ToString(), s.CompletedAtUtc, s.ActualDurationMinutes, s.ReviewNotes)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
