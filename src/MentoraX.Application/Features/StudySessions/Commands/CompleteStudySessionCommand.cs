using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Scheduling;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.Common.Validation;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.StudyPlans.Commands;
using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace MentoraX.Application.Features.StudySessions.Commands;

public sealed record CompleteStudySessionCommand
(
    Guid StudySessionId,
    int QualityScore,
    int DifficultyScore,
    int ActualDurationMinutes,
    string? ReviewNotes
) : ICommand<StudySessionDto>;
public sealed class CompleteStudySessionCommandHandler(IApplicationDbContext _dbContext,
    IStudyScheduleEngine _scheduleEngine,
    ICurrentUserService _currentUserService) 
    : ICommandHandler<CompleteStudySessionCommand, StudySessionDto>
{
    public async Task<StudySessionDto> Handle(CompleteStudySessionCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var session = await _dbContext.StudySessions
            .Include(x => x.StudyProgress)
            .Include(x=>x.StudyPlan)
            .FirstOrDefaultAsync(x =>
                x.Id == command.StudySessionId &&
                x.UserId == userId, cancellationToken);

        if (session is null)
            throw new AppNotFoundException("Study session not found.", "study_session_not_found");

        if (session.IsCompleted)
            throw new AppConflictException("Study session already completed.");

        var now = DateTime.UtcNow;

        session.MarkCompleted(
            command.QualityScore,
            command.DifficultyScore,
            command.ActualDurationMinutes,
            command.ReviewNotes,
            now);

        var progress = session.StudyProgress;

        var progressResult = _scheduleEngine.CalculateNext(
            progress.RepetitionCount,
            progress.IntervalDays,
            progress.EasinessFactor,
            command.QualityScore,
            command.DifficultyScore,
            now);

        progress.RepetitionCount = progressResult.RepetitionCount;
        progress.IntervalDays = progressResult.IntervalDays;
        progress.EasinessFactor = progressResult.EasinessFactor;
        progress.LastReviewedAtUtc = now;
        progress.NextReviewAtUtc = progressResult.NextReviewAtUtc;
        progress.UpdatedAtUtc = now;

        if (progressResult.IsFailure)
        {
            progress.FailureCount += 1;
            progress.SuccessStreak = 0;
        }
        else
        {
            progress.SuccessStreak += progressResult.SuccessStreakDelta;
        }

        var nextSession = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyPlanId = session.StudyPlanId,
            LearningMaterialId = session.LearningMaterialId,
            UserId = session.UserId,
            StudyProgressId = progress.Id,
            ScheduledAtUtc = progress.NextReviewAtUtc,
            IsCompleted = false,
            Order = session.Order + 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.StudySessions.Add(nextSession);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StudySessionDto(
            session.Id,
            session.StudyPlanId,
            session.Order,
            session.ScheduledAtUtc,
            session.StudyPlan.DailyTargetMinutes,
            "Completed",
            session.CompletedAtUtc,
            session.ActualDurationMinutes,
            session.ReviewNotes
        );
    }
}