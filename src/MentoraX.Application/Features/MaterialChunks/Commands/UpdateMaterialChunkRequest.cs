namespace MentoraX.Application.Features.MaterialChunks.Commands;

public sealed record UpdateMaterialChunkRequest(
    string? Title,
    string Content,
    string? Summary,
    string? Keywords,
    int DifficultyLevel,
    int EstimatedStudyMinutes
);