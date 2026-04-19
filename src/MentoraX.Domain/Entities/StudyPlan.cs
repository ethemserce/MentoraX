using MentoraX.Domain.Enums;

namespace MentoraX.Domain.Entities;

public sealed class StudyPlan : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid LearningMaterialId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public int DailyTargetMinutes { get; private set; }
    public PlanStatus Status { get; set; } = PlanStatus.Active;

    public User? User { get; private set; }
    public LearningMaterial? LearningMaterial { get; private set; }

    private readonly List<StudySession> _sessions = [];
    //public IReadOnlyCollection<StudySession> Sessions => _sessions;
    public ICollection<StudyProgress> StudyProgresses { get; set; } = new List<StudyProgress>();
    public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();

    private StudyPlan() { }

    public StudyPlan(Guid userId, Guid learningMaterialId, string title, DateOnly startDate, int dailyTargetMinutes)
    {
        UserId = userId;
        LearningMaterialId = learningMaterialId;
        Title = title;
        StartDate = startDate;
        DailyTargetMinutes = dailyTargetMinutes;
        Status = PlanStatus.Active;
    }

    public void AddSession(StudySession session) => _sessions.Add(session);
    public void Complete() => Status = PlanStatus.Completed;
}
