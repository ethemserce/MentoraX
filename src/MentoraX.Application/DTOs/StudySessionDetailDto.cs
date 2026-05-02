namespace MentoraX.Application.DTOs;

public sealed record StudySessionDetailDto(
    Guid Id,
    Guid StudyPlanId,
    Guid? StudyPlanItemId,
    Guid LearningMaterialId,
    Guid? MaterialChunkId,
    string PlanTitle,
    string MaterialTitle,
    string? ChunkTitle,
    string? ChunkContent,
    string? ItemType,
    int SequenceNumber,
    DateTime ScheduledAtUtc,
    DateTime? StartedAtUtc,
    int PlannedDurationMinutes,
    string Status,
    DateTime? CompletedAtUtc,
    int? ActualDurationMinutes,
    string? Notes
);
