using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using MentoraX.Domain.Enums;

namespace MentoraX.Application.Features.Materials.Commands;

public sealed record CreateMaterialCommand(string Title, string MaterialType, string Content, int EstimatedDurationMinutes, string? Description, string? Tags) : ICommand<MaterialDto>;

public sealed class CreateMaterialCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateMaterialCommand, MaterialDto>
{
    public async Task<MaterialDto> Handle(CreateMaterialCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var materialType = Enum.Parse<MaterialType>(command.MaterialType, true);

        var entity = new LearningMaterial(
            userId,
            command.Title,
            materialType,
            command.Content,
            command.EstimatedDurationMinutes,
            command.Description,
            command.Tags);

        dbContext.LearningMaterials.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var chunk = new MaterialChunk(
            learningMaterialId: entity.Id,
            orderNo: 1,
            content: entity.Content,
            title: entity.Title,
            summary: entity.Description,
            keywords: entity.Tags,
            difficultyLevel: 1,
            estimatedStudyMinutes: entity.EstimatedDurationMinutes,
            isGeneratedByAI: false
        );

        dbContext.MaterialChunks.Add(chunk);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MaterialDto(
            entity.Id,
            entity.UserId,
            entity.Title,
            entity.MaterialType.ToString(),
            entity.Content,
            entity.EstimatedDurationMinutes,
            entity.Description,
            entity.Tags,
            false,
            null,
            null
        );
    }
}