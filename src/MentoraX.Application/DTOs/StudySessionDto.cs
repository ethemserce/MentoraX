namespace MentoraX.Application.DTOs;

public sealed record StudySessionDto
{
    public Guid Id { get; set; }
    public Guid StudyPlanId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public string Status { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public string? Notes { get; set; }
    public double? EasinessFactor { get; set; }
    public int? IntervalDays { get; set; }
    public int? RepetitionCount { get; set; }

    private StudySessionDto()
    {

    }

    public StudySessionDto(Guid id, Guid studyPlanId, int sequenceNumber, DateTime scheduledAtUtc, int plannedDurationMinutes, string status, DateTime? completedAtUtc = null, int? actualDurationMinutes = null, string? notes = null, double? easinessFactor = null, int? intervalDays = null, int? repetitionCount = null)
    {
        Id = id;
        StudyPlanId = studyPlanId;
        SequenceNumber = sequenceNumber;
        ScheduledAtUtc = scheduledAtUtc;
        PlannedDurationMinutes = plannedDurationMinutes;
        Status = status;
        CompletedAtUtc = completedAtUtc;
        ActualDurationMinutes = actualDurationMinutes;
        Notes = notes;
        EasinessFactor = easinessFactor;
        IntervalDays = intervalDays;
        RepetitionCount = repetitionCount;
    }
}