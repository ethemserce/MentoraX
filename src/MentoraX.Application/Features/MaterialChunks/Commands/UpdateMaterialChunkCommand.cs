using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.MaterialChunks.Commands;

public sealed record UpdateMaterialChunkCommand(
    Guid LearningMaterialId,
    Guid ChunkId,
    string? Title,
    string Content,
    string? Summary,
    string? Keywords,
    int DifficultyLevel,
    int EstimatedStudyMinutes
) : ICommand<MaterialChunkDto>;

public sealed class UpdateMaterialChunkCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateMaterialChunkCommand, MaterialChunkDto>
{
    public async Task<MaterialChunkDto> Handle(
        UpdateMaterialChunkCommand command,
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

        if (string.IsNullOrWhiteSpace(command.Content))
        {
            throw new AppConflictException(
                "Chunk content cannot be empty.",
                "chunk_content_required");
        }

        if (command.DifficultyLevel < 1 || command.DifficultyLevel > 5)
        {
            throw new AppConflictException(
                "Difficulty level must be between 1 and 5.",
                "invalid_difficulty_level");
        }

        if (command.EstimatedStudyMinutes <= 0)
        {
            throw new AppConflictException(
                "Estimated study minutes must be greater than zero.",
                "invalid_estimated_study_minutes");
        }

        chunk.Update(
            content: command.Content.Trim(),
            title: string.IsNullOrWhiteSpace(command.Title)
                ? null
                : command.Title.Trim(),
            summary: string.IsNullOrWhiteSpace(command.Summary)
                ? null
                : command.Summary.Trim(),
            keywords: string.IsNullOrWhiteSpace(command.Keywords)
                ? null
                : command.Keywords.Trim(),
            difficultyLevel: command.DifficultyLevel,
            estimatedStudyMinutes: command.EstimatedStudyMinutes
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return new MaterialChunkDto(
            chunk.Id,
            chunk.LearningMaterialId,
            chunk.OrderNo,
            chunk.Title,
            chunk.Content,
            chunk.Summary,
            chunk.Keywords,
            chunk.DifficultyLevel,
            chunk.EstimatedStudyMinutes,
            chunk.CharacterCount,
            chunk.IsGeneratedByAI
        );
    }
}