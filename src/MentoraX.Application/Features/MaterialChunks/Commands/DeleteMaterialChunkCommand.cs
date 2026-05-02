using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.MaterialChunks.Commands;

public sealed record DeleteMaterialChunkCommand(
    Guid LearningMaterialId,
    Guid ChunkId
) : ICommand<int>;

public sealed class DeleteMaterialChunkCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeleteMaterialChunkCommand, int>
{
    public async Task<int> Handle(
        DeleteMaterialChunkCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var chunk = await dbContext.MaterialChunks
            .Include(x => x.LearningMaterial)
            .FirstOrDefaultAsync(
                x => x.Id == command.ChunkId &&
                     x.LearningMaterialId == command.LearningMaterialId &&
                     x.LearningMaterial.UserId == userId,
                cancellationToken);

        if (chunk is null)
        {
            throw new AppNotFoundException(
                "Material chunk was not found.",
                "material_chunk_not_found");
        }

        var isUsedInPlan = await dbContext.StudyPlanItems
            .AnyAsync(
                x => x.MaterialChunkId == chunk.Id,
                cancellationToken);

        if (isUsedInPlan)
        {
            throw new AppConflictException(
                "This chunk is used in a study plan and cannot be deleted.",
                "chunk_is_used_in_study_plan");
        }

        var deletedAtUtc = DateTime.UtcNow;

        dbContext.SyncTombstones.Add(new SyncTombstone(
            userId,
            "MaterialChunk",
            chunk.Id,
            deletedAtUtc,
            "{}"));

        chunk.LearningMaterial.Touch();
        dbContext.MaterialChunks.Remove(chunk);
        await dbContext.SaveChangesAsync(cancellationToken);

        var remainingChunks = await dbContext.MaterialChunks
            .Where(x => x.LearningMaterialId == command.LearningMaterialId)
            .OrderBy(x => x.OrderNo)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < remainingChunks.Count; i++)
        {
            remainingChunks[i].ChangeOrderTemporary(-(i + 1));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < remainingChunks.Count; i++)
        {
            remainingChunks[i].ChangeOrder(i + 1);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return 1;
    }
}
