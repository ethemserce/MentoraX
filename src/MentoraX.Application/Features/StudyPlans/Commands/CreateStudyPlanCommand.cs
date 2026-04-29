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
    int? PreferredMinute,
    IReadOnlyCollection<int>? DayOffsets
) : ICommand<StudyPlanDto>;

public sealed class CreateStudyPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateStudyPlanCommand, StudyPlanDto>
{
    public async Task<StudyPlanDto> Handle(
        CreateStudyPlanCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var material = await dbContext.LearningMaterials
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == command.LearningMaterialId &&
                     x.UserId == userId,
                cancellationToken);

        if (material is null)
        {
            throw new AppNotFoundException(
                "Learning material was not found for the user.",
                "learning_material_not_found");
        }

        var chunks = await dbContext.MaterialChunks
            .AsNoTracking()
            .Where(x => x.LearningMaterialId == material.Id)
            .OrderBy(x => x.OrderNo)
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            throw new AppConflictException(
                "No material chunks found for this material.",
                "material_chunks_not_found");
        }

        var now = DateTime.UtcNow;

        var preferredHour = command.PreferredHour ?? 20;
        var preferredMinute = command.PreferredMinute ?? 0;

        if (preferredHour < 0 || preferredHour > 23)
        {
            throw new AppConflictException(
                "Preferred hour must be between 0 and 23.",
                "invalid_preferred_hour");
        }

        if (preferredMinute < 0 || preferredMinute > 59)
        {
            throw new AppConflictException(
                "Preferred minute must be between 0 and 59.",
                "invalid_preferred_minute");
        }

        var plan = new StudyPlan(
            userId,
            material.Id,
            command.Title,
            command.StartDate,
            command.DailyTargetMinutes)
        {
            Status = PlanStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.StudyPlans.Add(plan);

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
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.StudyProgresses.Add(progress);

        var studyPlanItems = new List<StudyPlanItem>();
        var sessions = new List<StudySession>();

        var dayOffsets = command.DayOffsets is { Count: > 0 }
            ? command.DayOffsets.OrderBy(x => x).ToList()
            : Enumerable.Range(0, chunks.Count).ToList();

        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];

            var offset = index < dayOffsets.Count
                ? dayOffsets[index]
                : dayOffsets.Last() + (index - dayOffsets.Count + 1);

            var plannedDate = command.StartDate.AddDays(offset);

            var localDateTime = plannedDate.ToDateTime(
                new TimeOnly(preferredHour, preferredMinute),
                DateTimeKind.Local);

            var plannedDateUtc = localDateTime.ToUniversalTime();

            if (index == 0)
            {
                progress.NextReviewAtUtc = plannedDateUtc;
            }

            var orderNo = index + 1;

            var item = new StudyPlanItem(
                studyPlanId: plan.Id,
                materialChunkId: chunk.Id,
                title: chunk.Title ?? $"{material.Title} - Part {orderNo}",
                description: chunk.Summary,
                itemType: StudyItemType.NewStudy,
                orderNo: orderNo,
                plannedDateUtc: plannedDateUtc,
                plannedStartTime: new TimeSpan(preferredHour, preferredMinute, 0),
                plannedEndTime: new TimeSpan(preferredHour, preferredMinute, 0)
                    .Add(TimeSpan.FromMinutes(chunk.EstimatedStudyMinutes)),
                durationMinutes: chunk.EstimatedStudyMinutes > 0
                    ? chunk.EstimatedStudyMinutes
                    : command.DailyTargetMinutes,
                priority: 1,
                isMandatory: true,
                sourceReason: "Generated from material chunk"
            );

            var session = new StudySession
            {
                Id = Guid.NewGuid(),
                StudyPlanId = plan.Id,
                StudyPlanItemId = item.Id,
                LearningMaterialId = material.Id,
                UserId = userId,
                StudyProgressId = progress.Id,
                ScheduledAtUtc = plannedDateUtc,
                IsCompleted = false,
                Order = orderNo,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            dbContext.StudyPlanItems.Add(item);
            dbContext.StudySessions.Add(session);

            studyPlanItems.Add(item);
            sessions.Add(session);
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
                .OrderBy(x => x.Order)
                .Select(x => new StudySessionDto(
                    x.Id,
                    x.StudyPlanId,
                    x.Order,
                    x.ScheduledAtUtc,
                    plan.DailyTargetMinutes,
                    x.IsCompleted ? "Completed" : "Planned",
                    x.CompletedAtUtc,
                    x.ActualDurationMinutes,
                    x.ReviewNotes
                ))
                .ToList(),

            studyPlanItems
                .OrderBy(x => x.OrderNo)
                .Select(item =>
                {
                    var chunk = chunks.FirstOrDefault(x => x.Id == item.MaterialChunkId);

                    return new StudyPlanItemDto(
                        item.Id,
                        item.StudyPlanId,
                        item.MaterialChunkId,
                        item.Title,
                        item.Description,
                        item.ItemType.ToString(),
                        item.OrderNo,
                        item.PlannedDateUtc,
                        item.PlannedStartTime,
                        item.PlannedEndTime,
                        item.DurationMinutes,
                        item.Status.ToString(),
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
                                chunk.EstimatedStudyMinutes,
                                chunk.CharacterCount,
                                chunk.IsGeneratedByAI
                            ),
                        sessions
                            .Where(s => s.StudyPlanItemId == item.Id)
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