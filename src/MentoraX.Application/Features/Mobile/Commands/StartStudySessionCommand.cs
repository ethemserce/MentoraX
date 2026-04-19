using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.Common.Validation;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Mobile.Commands;

public sealed record StartStudySessionCommand(Guid SessionId)
    : ICommand<NextStudySessionDto>;

public sealed class StartStudySessionCommandHandler(IApplicationDbContext _dbContext, 
    ICurrentUserService _currentUserService)
    : ICommandHandler<StartStudySessionCommand, NextStudySessionDto>
{
    public async Task<NextStudySessionDto> Handle(
        StartStudySessionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var session = await _dbContext.StudySessions
            .FirstOrDefaultAsync(x =>
                x.Id == command.SessionId &&
                x.UserId == userId,
                cancellationToken);

        if (session is null)
            throw new AppNotFoundException("Study session not found.", "study_session_not_found");

        if (session.IsCompleted)
            throw new AppConflictException("Completed sessions cannot be started.");

        if (!session.StartedAtUtc.HasValue)
        {
            session.MarkStarted(DateTime.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var material = await _dbContext.LearningMaterials
            .AsNoTracking()
            .FirstAsync(x => x.Id == session.LearningMaterialId, cancellationToken);

        return new NextStudySessionDto(
            session.Id,
            session.StudyPlanId,
            material.Id,
            material.Title,
            session.ScheduledAtUtc,
            session.StartedAtUtc,
            material.EstimatedDurationMinutes,
            session.ScheduledAtUtc <= DateTime.UtcNow);
    }
}