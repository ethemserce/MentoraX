using MentoraX.Application.Abstractions.Services;
using MentoraX.Domain.Entities;
using MentoraX.Domain.ValueObjects;

namespace MentoraX.Infrastructure.Services;

public sealed class StudyPlanGenerator : IStudyPlanGenerator
{
    public IReadOnlyCollection<StudySession> GenerateSessions(StudyPlan plan, 
        Guid userId,
        Guid learningMaterialId,
        Guid studyProgressId,
        int preferredHour, SpacedRepetitionRule rule)
    {
        var sessions = new List<StudySession>();

        var start = plan.StartDate
            .ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(preferredHour)), DateTimeKind.Local)
            .ToUniversalTime();

        var sequence = 1;

        foreach (var offset in rule.DayOffsets)
        {
            var sessionTime = start.AddDays(offset);

            var session = new StudySession
            {
                Id = Guid.NewGuid(),
                StudyPlanId = plan.Id,
                UserId = userId,
                LearningMaterialId = learningMaterialId,
                StudyProgressId = studyProgressId,
                ScheduledAtUtc = sessionTime,
                IsCompleted = false,
                Order = sequence++,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            plan.AddSession(session);
            sessions.Add(session);
        }

        return sessions;
    }
}
