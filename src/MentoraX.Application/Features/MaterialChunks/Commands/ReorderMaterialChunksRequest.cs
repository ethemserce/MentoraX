namespace MentoraX.Application.Features.MaterialChunks.Commands;

public sealed record ReorderMaterialChunksRequest(
    IReadOnlyCollection<Guid> ChunkIds
);