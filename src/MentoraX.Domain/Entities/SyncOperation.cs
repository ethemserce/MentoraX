namespace MentoraX.Domain.Entities;

public sealed class SyncOperation : BaseEntity
{
    private SyncOperation()
    {
    }

    public SyncOperation(
        Guid userId,
        string operationId,
        string operationType,
        string entityType,
        Guid entityId,
        string payload,
        DateTime occurredAtUtc,
        DateTime appliedAtUtc,
        string status,
        string? error = null)
    {
        UserId = userId;
        OperationId = operationId;
        OperationType = operationType;
        EntityType = entityType;
        EntityId = entityId;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
        AppliedAtUtc = appliedAtUtc;
        Status = status;
        Error = error;
        CreatedAtUtc = appliedAtUtc;
        UpdatedAtUtc = appliedAtUtc;
    }

    public Guid UserId { get; private set; }
    public string OperationId { get; private set; } = string.Empty;
    public string OperationType { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? Error { get; private set; }

    public User User { get; private set; } = null!;
}
