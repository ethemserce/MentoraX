namespace MentoraX.Application.DTOs;

public sealed record SyncChangesDto(
    DateTime ServerTimeUtc,
    IReadOnlyCollection<SyncChangeDto> Changes);
