using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Scheduling;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudySessions.Commands;

public sealed record CompleteStudySessionCommand(
    Guid StudySessionId,
    int QualityScore,
    int DifficultyScore,
    int ActualDurationMinutes,
    string? ReviewNotes
) : ICommand<StudySessionDto>;

public sealed class CompleteStudySessionCommandHandler(
    IApplicationDbContext _dbContext,
    IStudyScheduleEngine _scheduleEngine,
    ICurrentUserService _currentUserService)
    : ICommandHandler<CompleteStudySessionCommand, StudySessionDto>
{
    public async Task<StudySessionDto> Handle(
        CompleteStudySessionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var session = await _dbContext.StudySessions
            .Include(x => x.StudyProgress)
            .Include(x => x.StudyPlan)
            .Include(x => x.StudyPlanItem)
            .FirstOrDefaultAsync(
                x => x.Id == command.StudySessionId &&
                     x.UserId == userId,
                cancellationToken);

        if (session is null)
        {
            throw new AppNotFoundException(
                "Study session not found.",
                "study_session_not_found");
        }

        if (session.StudyPlan is null)
        {
            throw new AppConflictException(
                "Session does not belong to a valid study plan.",
                "session_plan_not_found");
        }

        if (session.StudyPlan.Status != PlanStatus.Active)
        {
            throw new AppConflictException(
               "This plan is no longer active. Please refresh the page.",
               "study_plan_not_active");
        }

        if (session.IsCompleted)
        {
            throw new AppConflictException(
                "Session has already been completed.",
                "session_already_completed");
        }

        var now = DateTime.UtcNow;

        if (session.ScheduledAtUtc > now)
        {
            throw new AppConflictException(
                "This session is scheduled for later.",
                "study_session_not_due_yet");
        }

        var expectedDurationMinutes = session.StudyPlan.DailyTargetMinutes > 0
            ? session.StudyPlan.DailyTargetMinutes
            : 1;

        var minimumRequiredMinutes = (int)Math.Ceiling(expectedDurationMinutes * 0.5);

        if (minimumRequiredMinutes > 5)
        {
            minimumRequiredMinutes = 5;
        }

        if (minimumRequiredMinutes < 1)
        {
            minimumRequiredMinutes = 1;
        }

        if (command.ActualDurationMinutes < minimumRequiredMinutes)
        {
            throw new AppConflictException(
                $"You studied for only {command.ActualDurationMinutes} minute(s). Please continue a little more before completing this session.",
                "actual_duration_too_short");
        }

        session.MarkCompleted(
            command.QualityScore,
            command.DifficultyScore,
            command.ActualDurationMinutes,
            command.ReviewNotes,
            now);

        if (session.StudyPlanItem is not null)
        {
            session.StudyPlanItem.MarkCompleted();
        }

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

        var maxItemOrder = await _dbContext.StudyPlanItems
            .Where(x => x.StudyPlanId == session.StudyPlanId)
            .Select(x => (int?)x.OrderNo)
            .MaxAsync(cancellationToken) ?? 0;

        var maxSessionOrder = await _dbContext.StudySessions
            .Where(x => x.StudyPlanId == session.StudyPlanId)
            .Select(x => (int?)x.Order)
            .MaxAsync(cancellationToken) ?? 0;

        var nextItemOrder = maxItemOrder + 1;
        var nextSessionOrder = maxSessionOrder + 1;

        var nextPlanItem = new StudyPlanItem(
            studyPlanId: session.StudyPlanId,
            materialChunkId: session.StudyPlanItem?.MaterialChunkId,
            title: session.StudyPlanItem?.Title ?? session.StudyPlan.Title,
            description: session.StudyPlanItem?.Description,
            itemType: StudyItemType.Repetition,
            orderNo: nextItemOrder,
            plannedDateUtc: progress.NextReviewAtUtc,
            plannedStartTime: null,
            plannedEndTime: null,
            durationMinutes: session.StudyPlan.DailyTargetMinutes,
            priority: 1,
            isMandatory: true,
            sourceReason: "Generated from repetition schedule"
        );

        var nextSession = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyPlanId = session.StudyPlanId,
            StudyPlanItemId = nextPlanItem.Id,
            LearningMaterialId = session.LearningMaterialId,
            UserId = session.UserId,
            StudyProgressId = progress.Id,
            ScheduledAtUtc = progress.NextReviewAtUtc,
            IsCompleted = false,
            Order = nextSessionOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.StudyPlanItems.Add(nextPlanItem);
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