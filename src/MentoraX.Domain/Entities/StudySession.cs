using MentoraX.Domain.Enums;

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

    public int? QualityScore { get; set; }          // 0-5
    public int? DifficultyScore { get; set; }       // 1-5
    public int? ActualDurationMinutes { get; set; }
    public string? ReviewNotes { get; set; }



    public StudyPlan StudyPlan { get; set; } = null!;
    public LearningMaterial LearningMaterial { get; set; } = null!;
    public User User { get; set; } = null!;
    public StudyProgress StudyProgress { get; set; } = null!;

    public StudySession() { }

    public StudySession(Guid studyPlanId, int sequenceNumber, DateTime scheduledAtUtc, int plannedDurationMinutes)
    {
        StudyPlanId = studyPlanId;
        Order = sequenceNumber;
        ScheduledAtUtc = scheduledAtUtc;
        ActualDurationMinutes = plannedDurationMinutes;
        Touch();
    }

    public void MarkCompleted(int actualDurationMinutes, string? notes)
    {
        StudyPlan.Status = PlanStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ActualDurationMinutes = actualDurationMinutes;
        ReviewNotes = notes;
        Touch();
    }
}
