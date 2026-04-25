using MentoraX.Domain.Enums;

namespace MentoraX.Domain.Entities;

public sealed class LearningMaterial : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public MaterialType MaterialType { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int EstimatedDurationMinutes { get; private set; }
    public string? Tags { get; private set; }

    public User? User { get; private set; }
    public ICollection<StudyProgress> StudyProgresses { get; set; } = new List<StudyProgress>();
    public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();
    public ICollection<StudyPlan> StudyPlans { get; private set; } = new List<StudyPlan>();
    public ICollection<MaterialChunk> MaterialChunks { get; private set; } = new List<MaterialChunk>();
    private LearningMaterial() { }

    public LearningMaterial(Guid userId, string title, MaterialType materialType, string content, int estimatedDurationMinutes, string? description = null, string? tags = null)
    {
        UserId = userId;
        Title = title;
        MaterialType = materialType;
        Content = content;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        Description = description;
        Tags = tags;
    }
}
