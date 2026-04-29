using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.MaterialChunks.Commands;

public sealed record ReorderMaterialChunksCommand(
    Guid LearningMaterialId,
    IReadOnlyCollection<Guid> ChunkIds
) : ICommand<IReadOnlyCollection<MaterialChunkDto>>;

public sealed class ReorderMaterialChunksCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<ReorderMaterialChunksCommand, IReadOnlyCollection<MaterialChunkDto>>
{
    public async Task<IReadOnlyCollection<MaterialChunkDto>> Handle(
        ReorderMaterialChunksCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var materialExists = await dbContext.LearningMaterials
            .AnyAsync(
                x => x.Id == command.LearningMaterialId && x.UserId == userId,
                cancellationToken);

        if (!materialExists)
        {
            throw new AppNotFoundException(
                "Learning material was not found.",
                "learning_material_not_found");
        }

        var requestedIds = command.ChunkIds
            .Distinct()
            .ToList();

        if (requestedIds.Count == 0)
        {
            throw new AppConflictException(
                "Chunk order list cannot be empty.",
                "chunk_order_required");
        }

        var chunks = await dbContext.MaterialChunks
            .Where(x => x.LearningMaterialId == command.LearningMaterialId)
            .ToListAsync(cancellationToken);

        if (chunks.Count != requestedIds.Count)
        {
            throw new AppConflictException(
                "Chunk order list must contain all chunks for this material.",
                "invalid_chunk_order_count");
        }

        var existingIds = chunks.Select(x => x.Id).ToHashSet();

        if (requestedIds.Any(id => !existingIds.Contains(id)))
        {
            throw new AppConflictException(
                "Chunk order list contains invalid chunk ids.",
                "invalid_chunk_order_ids");
        }

        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i].ChangeOrderTemporary(-(i + 1));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < requestedIds.Count; i++)
        {
            var chunk = chunks.First(x => x.Id == requestedIds[i]);
            chunk.ChangeOrder(i + 1);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return chunks
            .OrderBy(x => x.OrderNo)
            .Select(x => new MaterialChunkDto(
                x.Id,
                x.LearningMaterialId,
                x.OrderNo,
                x.Title,
                x.Content,
                x.Summary,
                x.Keywords,
                x.DifficultyLevel,
                x.EstimatedStudyMinutes,
                x.CharacterCount,
                x.IsGeneratedByAI
            ))
            .ToList();
    }
}