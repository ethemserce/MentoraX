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
    public ICollection<StudyProgress> StudyProgresses { get; set; } = new List<StudyProgress>();
    public ICollection<StudySession> StudySessions { get; private set; } = new List<StudySession>();

    public void AddSession(StudySession session) => StudySessions.Add(session);

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

    public void Pause()
    {
        if (Status != PlanStatus.Active)
            throw new InvalidOperationException("Only active plans can be paused");

        Status = PlanStatus.Paused;
    }

    public void Resume()
    {
        if (Status != PlanStatus.Paused)
            throw new InvalidOperationException("Only paused plans can be resumed");

        Status = PlanStatus.Active;
    }

    public void Cancel()
    {
        if (Status == PlanStatus.Completed)
            throw new InvalidOperationException("Completed plan cannot be canceled");

        Status = PlanStatus.Canceled;
    }
    public void Complete() => Status = PlanStatus.Completed;
}
