using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthResponseDto>;

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasherService passwordHasherService,
    IJwtTokenGenerator jwtTokenGenerator) : ICommandHandler<LoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.IsActive, cancellationToken)
            ?? throw new AppUnauthorizedException("Invalid email or password.");

        var verified = passwordHasherService.VerifyPassword(user, user.PasswordHash, command.Password);
        if (!verified)
        {
            throw new AppUnauthorizedException("Invalid email or password.");
        }

        var token = jwtTokenGenerator.GenerateToken(user);

        var accessTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1);
        var refreshToken = string.Empty;
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        return new AuthResponseDto(user.Id, user.FullName, user.Email, token,accessTokenExpiresAtUtc,refreshToken,refreshTokenExpiresAtUtc);
    }
}
