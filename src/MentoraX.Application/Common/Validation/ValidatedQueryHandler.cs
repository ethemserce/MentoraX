using MentoraX.Application.Common;

namespace MentoraX.Application.Common.Validation;

public sealed class ValidatedQueryHandler<TQuery, TResponse>
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly IValidationPipeline _validationPipeline;
    private readonly IQueryHandler<TQuery, TResponse> _innerHandler;

    public ValidatedQueryHandler(
        IValidationPipeline validationPipeline,
        IQueryHandler<TQuery, TResponse> innerHandler)
    {
        _validationPipeline = validationPipeline;
        _innerHandler = innerHandler;
    }

    public async Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken)
    {
        await _validationPipeline.ValidateAsync(query, cancellationToken);
        return await _innerHandler.Handle(query, cancellationToken);
    }
}