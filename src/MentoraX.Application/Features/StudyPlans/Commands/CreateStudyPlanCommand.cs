using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed record CreateStudyPlanCommand(
    Guid LearningMaterialId,
    string Title,
    DateOnly StartDate,
    int DailyTargetMinutes,
    int? PreferredHour,
    IReadOnlyCollection<int>? DayOffsets) : ICommand<StudyPlanDto>;

public sealed class CreateStudyPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateStudyPlanCommand, StudyPlanDto>
{
    public async Task<StudyPlanDto> Handle(CreateStudyPlanCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var material = await dbContext.LearningMaterials
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == command.LearningMaterialId && x.UserId == userId,
                cancellationToken);

        if (material is null)
            throw new AppNotFoundException("Learning material was not found for the user.", "learning_material_not_found");

        var now = DateTime.UtcNow;
        var preferredHour = command.PreferredHour ?? 20;

        var scheduledAtUtc = command.StartDate
            .ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(preferredHour)), DateTimeKind.Local)
            .ToUniversalTime();

        var plan = new StudyPlan(userId, material.Id, command.Title, command.StartDate, command.DailyTargetMinutes)
        {
            Status = PlanStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var progress = new MentoraX.Domain.Entities.StudyProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LearningMaterialId = material.Id,
            StudyPlanId = plan.Id,
            RepetitionCount = 0,
            IntervalDays = 0,
            EasinessFactor = 2.5,
            SuccessStreak = 0,
            FailureCount = 0,
            NextReviewAtUtc = scheduledAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var firstSession = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyPlanId = plan.Id,
            LearningMaterialId = material.Id,
            UserId = userId,
            StudyProgressId = progress.Id,
            ScheduledAtUtc = scheduledAtUtc,
            IsCompleted = false,
            Order = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.StudyPlans.Add(plan);
        dbContext.StudyProgresses.Add(progress);
        dbContext.StudySessions.Add(firstSession);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new StudyPlanDto(
            plan.Id,
            plan.UserId,
            plan.LearningMaterialId,
            plan.Title,
            plan.StartDate,
            plan.DailyTargetMinutes,
            plan.Status.ToString(),
            new List<StudySessionDto>
            {
                new StudySessionDto(
                    firstSession.Id,
                    firstSession.StudyPlanId,
                    firstSession.Order,
                    firstSession.ScheduledAtUtc,
                    plan.DailyTargetMinutes,
                    "Planned",
                    firstSession.CompletedAtUtc,
                    firstSession.ActualDurationMinutes,
                    firstSession.ReviewNotes)
            });
    }
}