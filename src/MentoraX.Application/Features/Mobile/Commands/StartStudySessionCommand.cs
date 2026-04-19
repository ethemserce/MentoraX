using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using MentoraX.Application.Common.Validation;

namespace MentoraX.Application.Features.Mobile.Commands;

public sealed record StartStudySessionCommand(Guid SessionId, Guid UserId)
    : ICommand<NextStudySessionDto>;

public sealed class StartStudySessionCommandHandler(IApplicationDbContext _dbContext)
    : ICommandHandler<StartStudySessionCommand, NextStudySessionDto>
{
    public async Task<NextStudySessionDto> Handle(
        StartStudySessionCommand command,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.StudySessions
            .FirstOrDefaultAsync(x =>
                x.Id == command.SessionId &&
                x.UserId == command.UserId,
                cancellationToken);

        if (session is null)
            throw new AppNotFoundException("Study session not found.");

        if (session.IsCompleted)
            throw new InvalidOperationException("Completed sessions cannot be started.");

        if (!session.StartedAtUtc.HasValue)
        {
            session.StartedAtUtc = DateTime.UtcNow;
            session.UpdatedAtUtc = DateTime.UtcNow;
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