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
            .FirstOrDefaultAsync(x =>
                x.Id == command.StudySessionId &&
                x.UserId == userId,
                cancellationToken);

        if (session is null)
            throw new AppNotFoundException(
                "Study session not found.",
                "study_session_not_found");

        var now = DateTime.UtcNow;

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