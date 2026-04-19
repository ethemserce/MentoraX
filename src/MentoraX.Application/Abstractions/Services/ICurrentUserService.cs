namespace MentoraX.Application.Abstractions.Services;

public interface ICurrentUserService
{
    Guid GetRequiredUserId();
    Guid? GetUserId();
    string? GetEmail();
}
