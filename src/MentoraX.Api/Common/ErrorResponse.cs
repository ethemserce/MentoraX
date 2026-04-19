namespace MentoraX.Api.Common;

public sealed class ErrorResponse
{
    public ErrorDetail Error { get; set; } = new();

    public sealed class ErrorDetail
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public IReadOnlyCollection<ValidationErrorItem>? ValidationErrors { get; set; }
    }
}

public sealed class ValidationErrorItem
{
    public string Property { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}