using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudySessions.Commands;

public sealed record StartSessionCommand(Guid SessionId) : ICommand<StudySessionDto>;

public sealed class StartSessionCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<StartSessionCommand, StudySessionDto>
{
    public async Task<StudySessionDto> Handle(
        StartSessionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var session = await dbContext.StudySessions
            .Include(x => x.StudyPlan)
            .Include(x => x.StudyPlanItem)
            .FirstOrDefaultAsync(
                x => x.Id == command.SessionId && x.UserId == userId,
                cancellationToken);

        if (session is null)
            throw new AppNotFoundException(
                "Study session was not found.",
                "study_session_not_found");

        if (session.IsCompleted)
            throw new AppConflictException("Completed session cannot be started.",
                "session_already_completed");

        if (session.StudyPlanItem is not null)
        {
            session.StudyPlanItem.MarkInProgress();
        }

        session.StartedAtUtc ??= DateTime.UtcNow;
        session.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new StudySessionDto(
            session.Id,
            session.StudyPlanId,
            session.Order,
            session.ScheduledAtUtc,
            session.StudyPlan.DailyTargetMinutes,
            "InProgress",
            session.CompletedAtUtc,
            session.ActualDurationMinutes,
            session.ReviewNotes
        );
    }
}