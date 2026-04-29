using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.MaterialChunks.Queries;

public sealed record GetMaterialChunksQuery(Guid LearningMaterialId)
    : IQuery<IReadOnlyCollection<MaterialChunkDto>>;

public sealed class GetMaterialChunksQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMaterialChunksQuery, IReadOnlyCollection<MaterialChunkDto>>
{
    public async Task<IReadOnlyCollection<MaterialChunkDto>> Handle(
        GetMaterialChunksQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var materialExists = await dbContext.LearningMaterials
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == query.LearningMaterialId && x.UserId == userId,
                cancellationToken);

        if (!materialExists)
            return Array.Empty<MaterialChunkDto>();

        return await dbContext.MaterialChunks
            .AsNoTracking()
            .Where(x => x.LearningMaterialId == query.LearningMaterialId)
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
            .ToListAsync(cancellationToken);
    }
}