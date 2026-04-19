namespace MentoraX.Application.Abstractions.Scheduling;

public interface IStudyScheduleEngine
{
    StudyScheduleResult CalculateNext(
        int repetitionCount,
        int previousIntervalDays,
        double easinessFactor,
        int qualityScore,
        int difficultyScore,
        DateTime reviewedAtUtc);
}

public sealed record StudyScheduleResult(
    int RepetitionCount,
    int IntervalDays,
    double EasinessFactor,
    DateTime NextReviewAtUtc,
    int SuccessStreakDelta,
    bool IsFailure);