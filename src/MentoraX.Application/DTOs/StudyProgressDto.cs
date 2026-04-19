namespace MentoraX.Application.DTOs;

public sealed class StudyProgressDto
{
    public Guid MaterialId { get; set; }
    public int RepetitionCount { get; set; }
    public int IntervalDays { get; set; }
    public double EasinessFactor { get; set; }
    public int SuccessStreak { get; set; }
    public int FailureCount { get; set; }
    public DateTime NextReviewAtUtc { get; set; }

    public string PerformanceLevel { get; set; } = string.Empty;
    public string NextReviewReason { get; set; } = string.Empty;
}