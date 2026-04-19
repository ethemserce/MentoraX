using FluentValidation;

namespace MentoraX.Application.Features.Materials.Commands;

public sealed class CreateMaterialCommandValidator : AbstractValidator<CreateMaterialCommand>
{
    public CreateMaterialCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(200)
            .WithMessage("Title must be at most 200 characters.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required.");

        RuleFor(x => x.MaterialType)
            .NotEmpty()
            .WithMessage("MaterialType is invalid.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .InclusiveBetween(1, 600)
            .WithMessage("EstimatedDurationMinutes must be between 1 and 600.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage("Description must be at most 1000 characters.");

        RuleFor(x => x.Tags)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Tags))
            .WithMessage("Tags must be at most 500 characters.");
    }
}