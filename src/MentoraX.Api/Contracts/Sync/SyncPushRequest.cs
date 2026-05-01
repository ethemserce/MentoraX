using System.Text.Json;
using MentoraX.Application.DTOs;

namespace MentoraX.Api.Contracts.Sync;

public sealed record SyncPushRequest
{
    public IReadOnlyCollection<SyncPushOperationRequest> Operations { get; init; }
        = Array.Empty<SyncPushOperationRequest>();
}

public sealed record SyncPushOperationRequest
{
    public string? OperationId { get; init; }
    public string? Id { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public JsonElement Payload { get; init; }

    public SyncPushOperationDto ToDto()
    {
        return new SyncPushOperationDto(
            OperationId ?? Id ?? string.Empty,
            OperationType,
            EntityType,
            EntityId,
            OccurredAtUtc ?? CreatedAtUtc,
            NormalizePayload(Payload));
    }

    private static string NormalizePayload(JsonElement payload)
    {
        if (payload.ValueKind == JsonValueKind.Undefined ||
            payload.ValueKind == JsonValueKind.Null)
        {
            return "{}";
        }

        return payload.ValueKind == JsonValueKind.String
            ? payload.GetString() ?? "{}"
            : payload.GetRawText();
    }
}
