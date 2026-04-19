namespace MentoraX.Application.Common.Exceptions;

public sealed class AppConflictException : AppException
{
    public AppConflictException(string message)
        : base("conflict", message)
    {
    }
}