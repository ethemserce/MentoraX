using MentoraX.Domain.Entities;
using MentoraX.Domain.ValueObjects;

namespace MentoraX.Application.Abstractions.Services;

public interface IStudyPlanGenerator
{
    IReadOnlyCollection<StudySession> GenerateSessions(StudyPlan plan, Guid userId,
    Guid learningMaterialId,
    Guid studyProgressId, int preferredHour, SpacedRepetitionRule rule);
}
