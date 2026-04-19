namespace MentoraX.Application.Common.Exceptions;

public sealed class AppValidationException : AppException
{
    public IReadOnlyCollection<ValidationError> Errors { get; }

    public AppValidationException(IReadOnlyCollection<ValidationError> errors)
        : base("validation_error", "One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

public sealed class ValidationError
{
    public string Property { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}