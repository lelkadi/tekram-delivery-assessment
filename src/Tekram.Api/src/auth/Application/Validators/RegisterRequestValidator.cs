using FluentValidation;
using Tekram.Api.src.auth.Application.DTOs;

namespace Tekram.Api.src.auth.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private static readonly string[] AllowedRoles = ["customer", "driver", "merchant", "admin"];

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(@"^\+961[0-9]{7,8}$").WithMessage("Phone must be a valid Lebanese number starting with +961 (7-8 digits).");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Must(p => p.Any(char.IsDigit)).WithMessage("Password must contain at least one digit.")
            .Must(p => p.Any(char.IsUpper)).WithMessage("Password must contain at least one uppercase letter.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => AllowedRoles.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", AllowedRoles)}.");
    }
}
