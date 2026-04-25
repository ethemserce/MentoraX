using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudySessions.Queries;

public sealed record GetStudySessionByIdQuery(Guid SessionId)
    : IQuery<StudySessionDetailDto?>;

public sealed class GetStudySessionByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetStudySessionByIdQuery, StudySessionDetailDto?>
{
    public async Task<StudySessionDetailDto?> Handle(
        GetStudySessionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var session = await dbContext.StudySessions
            .AsNoTracking()
            .Include(x => x.StudyPlan)
            .Include(x => x.LearningMaterial)
            .Include(x => x.StudyPlanItem)
                .ThenInclude(x => x.MaterialChunk)
            .FirstOrDefaultAsync(
                x => x.Id == query.SessionId && x.UserId == userId,
                cancellationToken);

        if (session is null)
            return null;

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
            session.StudyPlan.DailyTargetMinutes,
            session.IsCompleted ? "Completed" : "Planned",
            session.CompletedAtUtc,
            session.ActualDurationMinutes,
            session.ReviewNotes
        );
    }
}