namespace MentoraX.Application.Common.Exceptions;

public sealed class AppNotFoundException : AppException
{
    public AppNotFoundException(string message, string code = "not_found")
        : base(code, message)
    {
    }
}