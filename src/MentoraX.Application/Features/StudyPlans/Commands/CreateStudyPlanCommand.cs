using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Validation;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using MentoraX.Domain.Enums;
using MentoraX.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed record CreateStudyPlanCommand(Guid UserId, Guid LearningMaterialId, string Title, DateOnly StartDate, int DailyTargetMinutes, int? PreferredHour, IReadOnlyCollection<int>? DayOffsets) : ICommand<StudyPlanDto>;

public sealed class CreateStudyPlanCommandHandler(IApplicationDbContext dbContext, 
    IStudyPlanGenerator studyPlanGenerator) : ICommandHandler<CreateStudyPlanCommand, StudyPlanDto>
{
    public async Task<StudyPlanDto> Handle(CreateStudyPlanCommand command, CancellationToken cancellationToken)
    {
        var material = await dbContext.LearningMaterials
              .AsNoTracking()
              .FirstOrDefaultAsync(
                  x => x.Id == command.LearningMaterialId && x.UserId == command.UserId,
                  cancellationToken);
        
        if (material is null) throw new InvalidOperationException("Learning material was not found for the user.");

        var now = DateTime.UtcNow;

        var plan = new StudyPlan(command.UserId, material.Id, command.Title, command.StartDate, command.DailyTargetMinutes);
        plan.Status = PlanStatus.Active;
        plan.CreatedAtUtc = now;
        plan.UpdatedAtUtc = now;

        var progress = new MentoraX.Domain.Entities.StudyProgress
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            LearningMaterialId = material.Id,
            StudyPlanId = plan.Id,
            RepetitionCount = 0,
            IntervalDays = 0,
            EasinessFactor = 2.5,
            SuccessStreak = 0,
            FailureCount = 0,
            NextReviewAtUtc = command.StartDate.ToDateTime(TimeOnly.MinValue),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var rule = command.DayOffsets is { Count: > 0 } ? new SpacedRepetitionRule(command.DayOffsets) : SpacedRepetitionRule.Default();
        var sessions = studyPlanGenerator.GenerateSessions(plan,command.UserId, command.LearningMaterialId, progress.Id, command.PreferredHour ?? 20, rule);

        dbContext.StudyPlans.Add(plan);
        dbContext.StudyProgresses.Add(progress);
        dbContext.StudySessions.AddRange(sessions);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new StudyPlanDto(plan.Id, plan.UserId, plan.LearningMaterialId, plan.Title, plan.StartDate, plan.DailyTargetMinutes, plan.Status.ToString(),
            sessions.OrderBy(x => x.Order).Select(x => new StudySessionDto(x.Id, x.StudyPlanId, x.Order, x.ScheduledAtUtc, x.StudyPlan.DailyTargetMinutes, x.StudyPlan.Status.ToString(), x.CompletedAtUtc, x.ActualDurationMinutes, x.ReviewNotes)).ToList());
    }
}
