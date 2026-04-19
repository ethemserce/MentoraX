using MentoraX.Domain.Entities;

namespace MentoraX.Application.Abstractions.Services;

public interface IPasswordHasherService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string hashedPassword, string providedPassword);
}
