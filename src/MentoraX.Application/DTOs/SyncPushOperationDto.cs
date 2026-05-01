namespace MentoraX.Application.DTOs;

public sealed record SyncPushOperationDto(
    string OperationId,
    string OperationType,
    string EntityType,
    Guid EntityId,
    DateTime? OccurredAtUtc,
    string Payload);
