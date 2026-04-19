namespace MentoraX.Application.DTOs;

public sealed record StudyPlanDto(Guid Id, Guid UserId, Guid LearningMaterialId, string Title, DateOnly StartDate, int DailyTargetMinutes, string Status, IReadOnlyCollection<StudySessionDto> Sessions);
