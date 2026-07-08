using FluentValidation;
using Tekram.Api.src.auth.Application.DTOs;

namespace Tekram.Api.src.auth.Application.Validators;

public class ResendOtpRequestValidator : AbstractValidator<ResendOtpRequest>
{
    private static readonly string[] AllowedChannels = ["email", "phone"];

    public ResendOtpRequestValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty().WithMessage("Channel is required.")
            .Must(channel => AllowedChannels.Contains(channel))
            .WithMessage("Channel must be 'email' or 'phone'.");
    }
}
