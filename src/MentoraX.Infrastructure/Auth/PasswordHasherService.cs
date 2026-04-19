using MentoraX.Application.Abstractions.Services;
using MentoraX.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace MentoraX.Infrastructure.Auth;

public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password)
        => _passwordHasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
        => _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword) != PasswordVerificationResult.Failed;
}
