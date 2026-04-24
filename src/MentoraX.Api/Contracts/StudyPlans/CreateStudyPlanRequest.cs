namespace MentoraX.Api.Contracts.StudyPlans;
public sealed record CreateStudyPlanRequest(Guid LearningMaterialId, string Title, DateOnly StartDate, int DailyTargetMinutes, int PreferredHour,int PreferredMinute, IReadOnlyCollection<int>? DayOffsets);
