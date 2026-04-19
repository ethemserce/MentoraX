namespace MentoraX.Api.Contracts.Mobile;

public sealed class CompleteMobileStudySessionRequest
{
    public int QualityScore { get; set; }
    public int DifficultyScore { get; set; }
    public int ActualDurationMinutes { get; set; }
    public string? ReviewNotes { get; set; }
}