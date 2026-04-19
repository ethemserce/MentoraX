namespace MentoraX.Application.Common.Exceptions;

public sealed class AppUnauthorizedException : AppException
{
    public AppUnauthorizedException(string message = "Unauthorized access.")
        : base("unauthorized", message)
    {
    }
}