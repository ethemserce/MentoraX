namespace MentoraX.Application.Common.Validation;

public interface IValidationService
{
    Task ValidateAndThrowAsync<T>(T instance, CancellationToken cancellationToken = default);
}