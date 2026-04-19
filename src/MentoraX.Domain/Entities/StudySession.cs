using MentoraX.Domain.Exceptions;

namespace MentoraX.Domain.Entities;

public sealed class StudySession : BaseEntity
{
    public Guid StudyPlanId { get; set; }
    public Guid LearningMaterialId { get; set; }
    public Guid UserId { get; set; }
    public Guid StudyProgressId { get; set; }

    public DateTime ScheduledAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public bool IsCompleted { get; set; }
    public int Order { get; set; }

    public int? QualityScore { get; set; }
    public int? DifficultyScore { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public string? ReviewNotes { get; set; }

    public StudyPlan StudyPlan { get; set; } = null!;
    public LearningMaterial LearningMaterial { get; set; } = null!;
    public User User { get; set; } = null!;
    public StudyProgress StudyProgress { get; set; } = null!;

    public void MarkStarted(DateTime startedAtUtc)
    {
        if (IsCompleted)
            throw new DomainConflictException("Completed sessions cannot be started.");

        if (!StartedAtUtc.HasValue)
        {
            StartedAtUtc = startedAtUtc;
            Touch();
        }
    }

    public void MarkCompleted(int qualityScore, int difficultyScore, int actualDurationMinutes, string? reviewNotes, DateTime completedAtUtc)
    {
        if (IsCompleted)
            throw new DomainConflictException("Study session already completed.");

        IsCompleted = true;
        CompletedAtUtc = completedAtUtc;
        QualityScore = qualityScore;
        DifficultyScore = difficultyScore;
        ActualDurationMinutes = actualDurationMinutes;
        ReviewNotes = reviewNotes;
        Touch();
    }
}