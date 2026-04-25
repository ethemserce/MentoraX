using FluentValidation;

namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed class CreateStudyPlanCommandValidator : AbstractValidator<CreateStudyPlanCommand>
{
    public CreateStudyPlanCommandValidator()
    {
        RuleFor(x => x.LearningMaterialId)
            .NotEmpty()
            .WithMessage("LearningMaterialId is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(200)
            .WithMessage("Title must be at most 200 characters.");

        RuleFor(x => x.DailyTargetMinutes)
            .InclusiveBetween(5, 600)
            .WithMessage("DailyTargetMinutes must be between 5 and 600.");

        RuleFor(x => x.PreferredHour)
            .InclusiveBetween(0, 23)
            .When(x => x.PreferredHour != null)
            .WithMessage("PreferredHour must be between 0 and 23.");

        RuleFor(x => x.PreferredMinute)
          .InclusiveBetween(0, 59)
          .When(x => x.PreferredMinute != null)
          .WithMessage("PreferredMinute must be between 0 and 59.");
    }
}