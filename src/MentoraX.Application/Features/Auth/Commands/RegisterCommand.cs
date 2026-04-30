using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Auth.Commands;

public sealed record RegisterCommand(string FullName, string Email, string Password, string? TimeZone) : ICommand<AuthResponseDto>;

public sealed class RegisterCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasherService passwordHasherService,
    IJwtTokenGenerator jwtTokenGenerator) : ICommandHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new AppConflictException("A user with this email already exists.");
        }

        var user = new User(command.FullName, normalizedEmail, string.Empty, command.TimeZone ?? "Europe/Istanbul");
        user.SetPasswordHash(passwordHasherService.HashPassword(user, command.Password));

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(user);

        var accessTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1);
        var refreshToken = string.Empty;
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        return new AuthResponseDto(user.Id, user.FullName, user.Email, token, accessTokenExpiresAtUtc, refreshToken, refreshTokenExpiresAtUtc);
    }
}
