using MentoraX.Application.Abstractions.Scheduling;

namespace MentoraX.Application.Services;

public sealed class StudyScheduleEngine : IStudyScheduleEngine
{
    public StudyScheduleResult CalculateNext(
        int repetitionCount,
        int previousIntervalDays,
        double easinessFactor,
        int qualityScore,
        int difficultyScore,
        DateTime reviewedAtUtc)
    {
        qualityScore = Math.Clamp(qualityScore, 0, 5);
        difficultyScore = Math.Clamp(difficultyScore, 1, 5);

        var ef = easinessFactor <= 0 ? 2.5 : easinessFactor;
        var adjustedRepetitionCount = repetitionCount;
        var intervalDays = previousIntervalDays;

        var failure = qualityScore < 3;

        // SM-2 inspired EF update
        ef = ef + (0.1 - (5 - qualityScore) * (0.08 + (5 - qualityScore) * 0.02));

        // difficulty penalty
        ef -= (difficultyScore - 3) * 0.05;

        if (ef < 1.3)
            ef = 1.3;

        if (failure)
        {
            adjustedRepetitionCount = 0;
            intervalDays = 1;
        }
        else
        {
            adjustedRepetitionCount++;

            if (adjustedRepetitionCount == 1)
                intervalDays = 1;
            else if (adjustedRepetitionCount == 2)
                intervalDays = 3;
            else
                intervalDays = Math.Max(1, (int)Math.Round(previousIntervalDays * ef));
        }

        var nextReviewAtUtc = reviewedAtUtc.AddDays(intervalDays);

        return new StudyScheduleResult(
            adjustedRepetitionCount,
            intervalDays,
            Math.Round(ef, 2),
            nextReviewAtUtc,
            failure ? 0 : 1,
            failure);
    }
}