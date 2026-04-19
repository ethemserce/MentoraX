namespace MentoraX.Api.Contracts.Materials;
public sealed record CreateMaterialRequest(string Title, string MaterialType, string Content, int EstimatedDurationMinutes, string? Description, string? Tags);
