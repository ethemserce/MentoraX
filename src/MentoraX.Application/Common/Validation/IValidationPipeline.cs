namespace MentoraX.Application.Common.Validation;

public interface IValidationPipeline
{
    Task ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default);
}