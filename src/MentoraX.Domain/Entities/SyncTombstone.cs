namespace MentoraX.Domain.Entities;

public sealed class SyncTombstone : BaseEntity
{
    private SyncTombstone()
    {
    }

    public SyncTombstone(
        Guid userId,
        string entityType,
        Guid entityId,
        DateTime deletedAtUtc,
        string payload = "{}")
    {
        UserId = userId;
        EntityType = entityType;
        EntityId = entityId;
        DeletedAtUtc = deletedAtUtc;
        Payload = payload;
        CreatedAtUtc = deletedAtUtc;
        UpdatedAtUtc = deletedAtUtc;
    }

    public Guid UserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTime DeletedAtUtc { get; private set; }
    public string Payload { get; private set; } = "{}";

    public User User { get; private set; } = null!;
}
