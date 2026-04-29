using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.MaterialChunks.Commands;

public sealed record CreateMaterialChunkCommand(
    Guid LearningMaterialId,
    string? Title,
    string Content,
    string? Summary,
    string? Keywords,
    int DifficultyLevel,
    int EstimatedStudyMinutes
) : ICommand<MaterialChunkDto>;

public sealed class CreateMaterialChunkCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateMaterialChunkCommand, MaterialChunkDto>
{
    public async Task<MaterialChunkDto> Handle(
        CreateMaterialChunkCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var materialExists = await dbContext.LearningMaterials
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == command.LearningMaterialId && x.UserId == userId,
                cancellationToken);

        if (!materialExists)
        {
            throw new AppNotFoundException(
                "Learning material was not found.",
                "learning_material_not_found");
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

        var nextOrderNo = await dbContext.MaterialChunks
            .Where(x => x.LearningMaterialId == command.LearningMaterialId)
            .Select(x => (int?)x.OrderNo)
            .MaxAsync(cancellationToken) ?? 0;

        nextOrderNo += 1;

        var chunk = new MaterialChunk(
            learningMaterialId: command.LearningMaterialId,
            orderNo: nextOrderNo,
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
            estimatedStudyMinutes: command.EstimatedStudyMinutes,
            isGeneratedByAI: false
        );

        dbContext.MaterialChunks.Add(chunk);

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