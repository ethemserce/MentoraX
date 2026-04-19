using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Mobile.Commands;

public sealed record RegisterMobileDeviceCommand(
    Guid UserId,
    string DeviceToken,
    string Platform) : ICommand<MobileDeviceDto>;

public sealed class RegisterMobileDeviceCommandHandler(IApplicationDbContext _dbContext)
    : ICommandHandler<RegisterMobileDeviceCommand, MobileDeviceDto>
{
    public async Task<MobileDeviceDto> Handle(
        RegisterMobileDeviceCommand command,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var existing = await _dbContext.MobileDevices
            .FirstOrDefaultAsync(x =>
                x.UserId == command.UserId &&
                x.DeviceToken == command.DeviceToken,
                cancellationToken);

        if (existing is not null)
        {
            existing.Platform = command.Platform;
            existing.UpdatedAtUtc = now;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new MobileDeviceDto(
                existing.Id,
                existing.DeviceToken,
                existing.Platform,
                existing.CreatedAtUtc,
                existing.UpdatedAtUtc.Value);
        }

        var device = new MobileDevice
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            DeviceToken = command.DeviceToken,
            Platform = command.Platform,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.MobileDevices.Add(device);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MobileDeviceDto(
            device.Id,
            device.DeviceToken,
            device.Platform,
            device.CreatedAtUtc,
            device.UpdatedAtUtc.Value);
    }
}