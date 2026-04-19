namespace MentoraX.Domain.Entities;

public sealed class StudyProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LearningMaterialId { get; set; }
    public Guid StudyPlanId { get; set; }

    public int RepetitionCount { get; set; }
    public double EasinessFactor { get; set; } = 2.5;
    public int IntervalDays { get; set; }
    public int SuccessStreak { get; set; }
    public int FailureCount { get; set; }

    public DateTime? LastReviewedAtUtc { get; set; }
    public DateTime NextReviewAtUtc { get; set; }

    public User User { get; set; } = null!;
    public LearningMaterial LearningMaterial { get; set; } = null!;
    public StudyPlan StudyPlan { get; set; } = null!;
    public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();

}