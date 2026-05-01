using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Mobile.Commands;

public sealed record StartStudySessionCommand(Guid SessionId)
    : ICommand<NextStudySessionDto>;

public sealed class StartStudySessionCommandHandler(
    IApplicationDbContext _dbContext,
    ICurrentUserService _currentUserService)
    : ICommandHandler<StartStudySessionCommand, NextStudySessionDto>
{
    public async Task<NextStudySessionDto> Handle(
        StartStudySessionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var now = DateTime.UtcNow;

        var session = await _dbContext.StudySessions
            .Include(x => x.StudyPlan)
            .Include(x => x.LearningMaterial)
            .FirstOrDefaultAsync(
                x => x.Id == command.SessionId &&
                     x.UserId == userId,
                cancellationToken);

        if (session is null)
        {
            throw new AppNotFoundException(
                "Study session not found.",
                "study_session_not_found");
        }

        if (session.IsCompleted)
        {
            throw new AppConflictException(
                "This session has already been completed.",
                "study_session_already_completed");
        }

        if (session.StudyPlan.Status != PlanStatus.Active)
        {
            throw new AppConflictException(
                "This plan is no longer active. Please refresh the page.",
                "study_plan_not_active");
        }

        if (session.ScheduledAtUtc > now)
        {
            throw new AppConflictException(
                "This session is scheduled for later.",
                "study_session_not_due_yet");
        }

        if (!session.StartedAtUtc.HasValue)
        {
            session.MarkStarted(now);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new NextStudySessionDto(
            session.Id,
            session.StudyPlanId,
            session.LearningMaterial.Id,
            session.LearningMaterial.Title,
            session.ScheduledAtUtc,
            session.StartedAtUtc,
            session.LearningMaterial.EstimatedDurationMinutes,
            session.ScheduledAtUtc <= now);
    }
}