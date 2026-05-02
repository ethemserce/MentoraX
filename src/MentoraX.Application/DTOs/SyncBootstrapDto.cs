namespace MentoraX.Application.DTOs;

public sealed record SyncBootstrapDto(
    DateTime ServerTimeUtc,
    IReadOnlyCollection<StudyPlanDto> StudyPlans,
    IReadOnlyCollection<MaterialDto> Materials,
    IReadOnlyCollection<MaterialChunkDto> MaterialChunks,
    MobileDashboardDto Dashboard,
    NextStudySessionDto? NextSession);
