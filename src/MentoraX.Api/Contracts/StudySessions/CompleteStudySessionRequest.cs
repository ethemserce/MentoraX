namespace MentoraX.Api.Contracts.StudySessions;

public sealed class CompleteStudySessionRequest
{
    public int QualityScore { get; set; }
    public int DifficultyScore { get; set; }
    public int ActualDurationMinutes { get; set; }
    public string? ReviewNotes { get; set; }
}