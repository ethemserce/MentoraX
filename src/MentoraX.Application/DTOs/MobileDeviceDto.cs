namespace MentoraX.Application.DTOs;

public sealed record MobileDeviceDto(
    Guid Id,
    string DeviceToken,
    string Platform,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);