using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Materials.Queries;

public sealed record GetMaterialByIdQuery(Guid Id) : IQuery<MaterialDto?>;

public sealed class GetMaterialByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMaterialByIdQuery, MaterialDto?>
{
    public async Task<MaterialDto?> Handle(
        GetMaterialByIdQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var material = await dbContext.LearningMaterials
            .AsNoTracking()
            .Where(x => x.Id == query.Id && x.UserId == userId)
            .Select(x => new
            {
                Material = x,
                ActivePlan = x.StudyPlans
                    .Where(p => p.Status == PlanStatus.Active || p.Status == PlanStatus.Paused)
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (material is null)
            return null;

        return new MaterialDto(
            material.Material.Id,
            material.Material.UserId,
            material.Material.Title,
            material.Material.Description,
            material.Material.Content,
            material.Material.EstimatedDurationMinutes,
            material.Material.MaterialType.ToString(),
            material.Material.Tags,
            material.ActivePlan is not null,
            material.ActivePlan != null ? material.ActivePlan.Id : null,
            material.ActivePlan != null ? material.ActivePlan.Title : null
        );
    }
}