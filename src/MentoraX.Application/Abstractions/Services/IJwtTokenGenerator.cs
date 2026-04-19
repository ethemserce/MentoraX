using MentoraX.Domain.Entities;

namespace MentoraX.Application.Abstractions.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
