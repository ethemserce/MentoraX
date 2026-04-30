namespace MentoraX.Application.DTOs;

public sealed record AuthResponseDto(
    Guid UserId,
    string FullName,
    string Email,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc
);
