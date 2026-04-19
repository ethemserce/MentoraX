using FluentValidation;

namespace MentoraX.Application.Features.StudySessions.Commands;

public sealed class CompleteStudySessionCommandValidator : AbstractValidator<CompleteStudySessionCommand>
{
    public CompleteStudySessionCommandValidator()
    {
        RuleFor(x => x.StudySessionId)
            .NotEmpty()
            .WithMessage("StudySessionId is required.");

        RuleFor(x => x.QualityScore)
            .InclusiveBetween(0, 5)
            .WithMessage("QualityScore must be between 0 and 5.");

        RuleFor(x => x.DifficultyScore)
            .InclusiveBetween(1, 5)
            .WithMessage("DifficultyScore must be between 1 and 5.");

        RuleFor(x => x.ActualDurationMinutes)
            .GreaterThan(0)
            .WithMessage("ActualDurationMinutes must be greater than 0.");
    }
}