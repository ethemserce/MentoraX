namespace MentoraX.Application.Common.Exceptions;

public sealed class AppForbiddenException : AppException
{
    public AppForbiddenException(string message = "Forbidden.")
        : base("forbidden", message)
    {
    }
}