namespace MentoraX.Application.DTOs;

public sealed record SyncPushOperationResultDto(
    string OperationId,
    string Status,
    string? Error);
