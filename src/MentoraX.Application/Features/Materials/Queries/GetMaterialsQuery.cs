using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Materials.Queries;

public sealed record GetMaterialsQuery(Guid? UserId) : IQuery<IReadOnlyCollection<MaterialDto>>;

public sealed class GetMaterialsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetMaterialsQuery, IReadOnlyCollection<MaterialDto>>
{
    public async Task<IReadOnlyCollection<MaterialDto>> Handle(
        GetMaterialsQuery query,
        CancellationToken cancellationToken)
    {
        var materialQuery = dbContext.LearningMaterials.AsNoTracking();

        if (query.UserId.HasValue)
            materialQuery = materialQuery.Where(x => x.UserId == query.UserId.Value);

        var result = await materialQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new MaterialDto(
                x.Id,
                x.UserId,
                x.Title,
                x.MaterialType.ToString(),
                x.Content,
                x.EstimatedDurationMinutes,
                x.Description,
                x.Tags,
                x.StudyPlans.Any(p => p.Status == PlanStatus.Active),
                x.StudyPlans
                    .Where(p => p.Status == PlanStatus.Active)
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .Select(p => (Guid?)p.Id)
                    .FirstOrDefault(),
                x.StudyPlans
                    .Where(p => p.Status == PlanStatus.Active)
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .Select(p => p.Title)
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);

        return result;
    }
}