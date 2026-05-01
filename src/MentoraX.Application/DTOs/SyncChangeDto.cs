namespace MentoraX.Application.DTOs;

public sealed record SyncChangeDto(
    string EntityType,
    Guid EntityId,
    string ChangeType,
    DateTime ChangedAtUtc);
