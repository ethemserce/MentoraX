namespace MentoraX.Application.DTOs;

public sealed record MaterialDto(Guid Id, Guid UserId, string Title, string MaterialType, string Content, int EstimatedDurationMinutes, string? Description, string? Tags);
