namespace MentoraX.Application.DTOs;

public sealed record AuthResponseDto(Guid UserId, string FullName, string Email, string Token);
