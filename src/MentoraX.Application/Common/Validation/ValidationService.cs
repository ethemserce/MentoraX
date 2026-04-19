using FluentValidation;
using MentoraX.Application.Common.Exceptions;

namespace MentoraX.Application.Common.Validation;

public sealed class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ValidateAndThrowAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<T>)) as IValidator<T>;
        if (validator is null)
            return;

        var result = await validator.ValidateAsync(instance, cancellationToken);

        if (result.IsValid)
            return;

        var errors = result.Errors
            .Select(x => new ValidationError
            {
                Property = x.PropertyName,
                Message = x.ErrorMessage
            })
            .ToList();

        throw new Exceptions.AppValidationException(errors);
    }
}