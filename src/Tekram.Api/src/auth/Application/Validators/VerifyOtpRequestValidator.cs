using FluentValidation;
using Tekram.Api.src.auth.Application.DTOs;

namespace Tekram.Api.src.auth.Application.Validators;

public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Matches(@"^[0-9]{6}$").WithMessage("Code must be exactly 6 digits.");
    }
}
