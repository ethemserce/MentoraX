namespace MentoraX.Application.Common.Exceptions;

public sealed class AppConflictException : AppException
{
    public AppConflictException(string message,string code = "conflict")
        : base(code, message)
    {
    }
}