using MentoraX.Application.Common;

namespace MentoraX.Application.Common.Validation;

public sealed class ValidatedCommandHandler<TCommand, TResponse>
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly IValidationPipeline _validationPipeline;
    private readonly ICommandHandler<TCommand, TResponse> _innerHandler;

    public ValidatedCommandHandler(
        IValidationPipeline validationPipeline,
        ICommandHandler<TCommand, TResponse> innerHandler)
    {
        _validationPipeline = validationPipeline;
        _innerHandler = innerHandler;
    }

    public async Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken)
    {
        await _validationPipeline.ValidateAsync(command, cancellationToken);
        return await _innerHandler.Handle(command, cancellationToken);
    }
}