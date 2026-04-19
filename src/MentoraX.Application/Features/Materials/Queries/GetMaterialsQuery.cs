using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Materials.Queries;

public sealed record GetMaterialsQuery(Guid? UserId) : IQuery<IReadOnlyCollection<MaterialDto>>;

public sealed class GetMaterialsQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetMaterialsQuery, IReadOnlyCollection<MaterialDto>>
{
    public async Task<IReadOnlyCollection<MaterialDto>> Handle(GetMaterialsQuery query, CancellationToken cancellationToken)
    {
        var materialQuery = dbContext.LearningMaterials.AsNoTracking();
        if (query.UserId.HasValue) materialQuery = materialQuery.Where(x => x.UserId == query.UserId.Value);
        return await materialQuery.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new MaterialDto(x.Id, x.UserId, x.Title, x.MaterialType.ToString(), x.Content, x.EstimatedDurationMinutes, x.Description, x.Tags))
            .ToListAsync(cancellationToken);
    }
}
