using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static System.Collections.Specialized.BitVector32;

namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed record CreateStudyPlanCommand(
    Guid LearningMaterialId,
    string Title,
    DateOnly StartDate,
    int DailyTargetMinutes,
    int PreferredHour,
    int PreferredMinute,
    IReadOnlyCollection<int>? DayOffsets) : ICommand<StudyPlanDto>;

public sealed class CreateStudyPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService _currentUserService)
    : ICommandHandler<CreateStudyPlanCommand, StudyPlanDto>
{
    public async Task<StudyPlanDto> Handle(CreateStudyPlanCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var material = await dbContext.LearningMaterials
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == command.LearningMaterialId && x.UserId == userId,
                cancellationToken);

        if (material is null)
            throw new AppNotFoundException("Learning material was not found for the user.", "learning_material_not_found");

        var now = DateTime.UtcNow;
        var preferredHour = command.PreferredHour;
        var preferredMinute = command.PreferredMinute;

        var scheduledAtUtc = command.StartDate
            .ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(preferredHour,preferredMinute)), DateTimeKind.Local)
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



        dbContext.StudyPlans.Add(plan);
        dbContext.StudyProgresses.Add(progress);


        await dbContext.SaveChangesAsync(cancellationToken);

        var chunks = await dbContext.MaterialChunks
    .Where(x => x.LearningMaterialId == command.LearningMaterialId)
    .OrderBy(x => x.OrderNo)
    .ToListAsync(cancellationToken);

        if (!chunks.Any())
        {
            throw new AppConflictException(
                "No material chunks found for this learning material."
            );
        }

        var orderNo = 1;

        var sessions = new List<StudySession>();
        var studyPlanItems = new List<StudyPlanItem>();

        foreach (var chunk in chunks)
        {
            var plannedDateUtc = DateTime.SpecifyKind(
                command.StartDate.ToDateTime(
                    new TimeOnly(command.PreferredHour, command.PreferredMinute)
                ),
                DateTimeKind.Local
            ).ToUniversalTime();

            var studyPlanItem = new StudyPlanItem(
                studyPlanId: plan.Id,
                materialChunkId: chunk.Id,
                title: chunk.Title ?? plan.Title,
                description: chunk.Summary,
                itemType: StudyItemType.NewStudy,
                orderNo: orderNo,
                plannedDateUtc: plannedDateUtc,
                plannedStartTime: new TimeSpan(command.PreferredHour, command.PreferredMinute, 0),
                plannedEndTime: new TimeSpan(command.PreferredHour, command.PreferredMinute, 0)
                    .Add(TimeSpan.FromMinutes(plan.DailyTargetMinutes)),
                durationMinutes: plan.DailyTargetMinutes,
                priority: 1,
                isMandatory: true,
                sourceReason: "Generated from material chunk"
            );

            var session = new StudySession
            {
                Id = Guid.NewGuid(),
                StudyPlanId = plan.Id,
                LearningMaterialId = material.Id,
                StudyPlanItemId = studyPlanItem.Id,
                UserId = userId,
                StudyProgressId = progress.Id,
                ScheduledAtUtc = plannedDateUtc,
                IsCompleted = false,
                Order = orderNo,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            studyPlanItem.StudySessions.Add(session);
            chunk.StudyPlanItems.Add(studyPlanItem);

            dbContext.StudySessions.Add(session);
            dbContext.StudyPlanItems.Add(studyPlanItem);

            studyPlanItems.Add(studyPlanItem);
            sessions.Add(session);
            orderNo++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new StudyPlanDto(
     plan.Id,
     plan.UserId,
     plan.LearningMaterialId,
     plan.Title,
     plan.StartDate,
     plan.DailyTargetMinutes,
     plan.Status.ToString(),

     sessions
         .OrderBy(s => s.Order)
         .Select(s => new StudySessionDto(
             s.Id,
             s.StudyPlanId,
             s.Order,
             s.ScheduledAtUtc,
             plan.DailyTargetMinutes,
             s.IsCompleted ? "Completed" : "Planned",
             s.CompletedAtUtc,
             s.ActualDurationMinutes,
             s.ReviewNotes
         ))
         .ToList(),

     studyPlanItems
         .OrderBy(i => i.OrderNo)
         .Select(i =>
         {
             var chunk = chunks.FirstOrDefault(c => c.Id == i.MaterialChunkId);

             return new StudyPlanItemDto(
                 i.Id,
                 i.StudyPlanId,
                 i.MaterialChunkId,
                 i.Title,
                 i.Description,
                 i.ItemType.ToString(),
                 i.OrderNo,
                 i.PlannedDateUtc,
                 i.PlannedStartTime,
                 i.PlannedEndTime,
                 i.DurationMinutes,
                 i.Status.ToString(),
                 chunk is null
                     ? null
                     : new MaterialChunkDto(
                         chunk.Id,
                         chunk.LearningMaterialId,
                         chunk.OrderNo,
                         chunk.Title,
                         chunk.Content,
                         chunk.Summary,
                         chunk.Keywords,
                         chunk.DifficultyLevel,
                         chunk.EstimatedStudyMinutes
                     ),
                 sessions
                     .Where(s => s.StudyPlanItemId == i.Id)
                     .OrderBy(s => s.Order)
                     .Select(s => new StudySessionDto(
                         s.Id,
                         s.StudyPlanId,
                         s.Order,
                         s.ScheduledAtUtc,
                         plan.DailyTargetMinutes,
                         s.IsCompleted ? "Completed" : "Planned",
                         s.CompletedAtUtc,
                         s.ActualDurationMinutes,
                         s.ReviewNotes
                     ))
                     .ToList()
             );
         })
         .ToList()
 );
    }
}