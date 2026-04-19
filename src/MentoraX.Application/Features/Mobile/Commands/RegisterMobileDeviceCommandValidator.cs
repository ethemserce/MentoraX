using FluentValidation;

namespace MentoraX.Application.Features.Mobile.Commands;

public sealed class RegisterMobileDeviceCommandValidator : AbstractValidator<RegisterMobileDeviceCommand>
{
    public RegisterMobileDeviceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.DeviceToken)
            .NotEmpty()
            .WithMessage("DeviceToken is required.")
            .MaximumLength(500)
            .WithMessage("DeviceToken must be at most 500 characters.");

        RuleFor(x => x.Platform)
            .NotEmpty()
            .WithMessage("Platform is required.")
            .Must(x => x == "android" || x == "ios")
            .WithMessage("Platform must be either 'android' or 'ios'.");
    }
}