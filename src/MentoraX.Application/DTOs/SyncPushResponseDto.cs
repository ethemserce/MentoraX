namespace MentoraX.Application.DTOs;

public sealed record SyncPushResponseDto(
    DateTime ServerTimeUtc,
    IReadOnlyCollection<SyncPushOperationResultDto> Results);
