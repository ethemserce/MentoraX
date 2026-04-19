using System.Security.Claims;
using MentoraX.Application.Abstractions.Services;
using Microsoft.AspNetCore.Http;

namespace MentoraX.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid GetRequiredUserId()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new InvalidOperationException("Authenticated user id could not be resolved.");
        }

        return userId.Value;
    }

    public Guid? GetUserId()
    {
        var rawValue = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

        return Guid.TryParse(rawValue, out var userId) ? userId : null;
    }

    public string? GetEmail() => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
}
