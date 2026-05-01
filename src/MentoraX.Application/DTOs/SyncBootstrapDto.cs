namespace MentoraX.Application.DTOs;

public sealed record SyncBootstrapDto(
    DateTime ServerTimeUtc,
    IReadOnlyCollection<StudyPlanDto> StudyPlans,
    MobileDashboardDto Dashboard,
    NextStudySessionDto? NextSession);
