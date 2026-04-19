namespace MentoraX.Application.DTOs;

public sealed record MobileProgressSummaryDto(
    int TotalMaterials,
    int ActivePlans,
    int StrongCount,
    int MediumCount,
    int WeakCount,
    int TodayCompletedSessions,
    int CurrentStreakDays
);