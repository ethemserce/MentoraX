namespace MentoraX.Application.DTOs;

public sealed record MobileDashboardDto(
    int DueCount,
    int TodayPlannedMinutes,
    int TodayCompletedMinutes,
    NextStudySessionDto? NextSession,
    IReadOnlyCollection<WeakMaterialDto> WeakMaterials
);