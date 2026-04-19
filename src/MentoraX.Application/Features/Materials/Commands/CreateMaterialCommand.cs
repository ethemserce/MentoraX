using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Validation;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using MentoraX.Domain.Enums;

namespace MentoraX.Application.Features.Materials.Commands;

public sealed record CreateMaterialCommand(Guid UserId, string Title, string MaterialType, string Content, int EstimatedDurationMinutes, string? Description, string? Tags) : ICommand<MaterialDto>;

public sealed class CreateMaterialCommandHandler(IApplicationDbContext dbContext, ICurrentUserService _currentUser) : ICommandHandler<CreateMaterialCommand, MaterialDto>
{
    public async Task<MaterialDto> Handle(CreateMaterialCommand command, CancellationToken cancellationToken)
    {
        var materialType = Enum.Parse<MaterialType>(command.MaterialType, true);
        var entity = new LearningMaterial(_currentUser.GetUserId().Value, command.Title, materialType, command.Content, command.EstimatedDurationMinutes, command.Description, command.Tags);
        dbContext.LearningMaterials.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new MaterialDto(entity.Id, entity.UserId, entity.Title, entity.MaterialType.ToString(), entity.Content, entity.EstimatedDurationMinutes, entity.Description, entity.Tags);
    }
}
