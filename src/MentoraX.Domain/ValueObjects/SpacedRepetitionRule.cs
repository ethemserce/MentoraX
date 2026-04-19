namespace MentoraX.Domain.ValueObjects;

public sealed class SpacedRepetitionRule
{
    public IReadOnlyList<int> DayOffsets { get; }

    public SpacedRepetitionRule(IEnumerable<int> dayOffsets)
    {
        var offsets = dayOffsets.Where(x => x >= 0).Distinct().OrderBy(x => x).ToList();
        if (offsets.Count == 0) throw new ArgumentException("At least one offset is required.", nameof(dayOffsets));
        DayOffsets = offsets;
    }

    public static SpacedRepetitionRule Default() => new([0, 1, 3, 7, 14, 30]);
}
