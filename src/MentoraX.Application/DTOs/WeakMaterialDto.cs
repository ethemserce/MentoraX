namespace MentoraX.Application.DTOs;

public sealed record WeakMaterialDto(
    Guid MaterialId,
    string Title,
    string PerformanceLevel,
    DateTime NextReviewAtUtc
);