using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
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

        return await _dbContext.StudySessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                !x.IsCompleted)
            .OrderBy(x => x.ScheduledAtUtc)
            .Join(
                _dbContext.LearningMaterials.AsNoTracking(),
                session => session.LearningMaterialId,
                material => material.Id,
                (session, material) => new NextStudySessionDto(
                    session.Id,
                    session.StudyPlanId,
                    material.Id,
                    material.Title,
                    session.ScheduledAtUtc,
                    session.StartedAtUtc,
                    material.EstimatedDurationMinutes,
                    session.ScheduledAtUtc <= now))
            .FirstOrDefaultAsync(cancellationToken);
    }
}