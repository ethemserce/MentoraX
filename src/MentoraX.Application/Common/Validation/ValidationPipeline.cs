using FluentValidation;
using MentoraX.Application.Common.Exceptions;

namespace MentoraX.Application.Common.Validation;

public sealed class ValidationPipeline : IValidationPipeline
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationPipeline(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<TRequest>)) as IValidator<TRequest>;
        if (validator is null)
            return;

        var result = await validator.ValidateAsync(request, cancellationToken);

        if (result.IsValid)
            return;

        var errors = result.Errors
            .Select(x => new ValidationError
            {
                Property = x.PropertyName,
                Message = x.ErrorMessage
            })
            .ToList();

        throw new AppValidationException(errors);
    }
}