namespace MentoraX.Application.DTOs;

public sealed record NextStudySessionDto(
    Guid SessionId,
    Guid StudyPlanId,
    Guid MaterialId,
    string MaterialTitle,
    DateTime ScheduledAtUtc,
    DateTime? StartedAtUtc,
    int EstimatedMinutes,
    bool IsDue
);