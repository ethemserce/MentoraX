using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Mobile.Queries;

public sealed record GetNextStudySessionQuery(Guid UserId) : IQuery<NextStudySessionDto?>;

public sealed class GetNextStudySessionQueryHandler(IApplicationDbContext _dbContext)
    : IQueryHandler<GetNextStudySessionQuery, NextStudySessionDto?>
{
    public async Task<NextStudySessionDto?> Handle(
    GetNextStudySessionQuery query,
    CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var session = await _dbContext.StudySessions
            .AsNoTracking()
            .Include(x => x.StudyPlan)
            .Include(x => x.LearningMaterial)
            .Where(x =>
                x.UserId == query.UserId &&
                !x.IsCompleted &&
                x.StudyPlan.Status == PlanStatus.Active)
            .OrderBy(x => x.ScheduledAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
            return null;

        return new NextStudySessionDto(
            session.Id,
            session.StudyPlanId,
            session.LearningMaterialId,
            session.LearningMaterial.Title,
            session.ScheduledAtUtc,
            session.StartedAtUtc,
            session.LearningMaterial.EstimatedDurationMinutes,
            session.ScheduledAtUtc <= now);
    }
}