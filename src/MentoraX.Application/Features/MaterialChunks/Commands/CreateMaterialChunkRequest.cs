namespace MentoraX.Application.Features.MaterialChunks.Commands;

public sealed record CreateMaterialChunkRequest(
    string? Title,
    string Content,
    string? Summary,
    string? Keywords,
    int DifficultyLevel,
    int EstimatedStudyMinutes
);