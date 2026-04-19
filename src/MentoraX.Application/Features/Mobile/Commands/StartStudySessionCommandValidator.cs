using FluentValidation;

namespace MentoraX.Application.Features.Mobile.Commands;

public sealed class StartStudySessionCommandValidator : AbstractValidator<StartStudySessionCommand>
{
    public StartStudySessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("SessionId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");
    }
}