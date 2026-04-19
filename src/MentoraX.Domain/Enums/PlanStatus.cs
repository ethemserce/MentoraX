namespace MentoraX.Domain.Enums;

public enum PlanStatus
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Archived = 4
}

public static class PlanStatusExtensions
{
    public static bool IsActive(this PlanStatus status)
        => status == PlanStatus.Active;
}