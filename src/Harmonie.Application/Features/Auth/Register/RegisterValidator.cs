using FluentValidation;

namespace Harmonie.Application.Features.Auth.Register;

/// <summary>
/// Validator for RegisterRequest
/// </summary>
public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email format is invalid")
            .MaximumLength(320)
            .WithMessage("Email is too long");

        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters")
            .MaximumLength(32)
            .WithMessage("Username cannot exceed 32 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Username can only contain letters, numbers, underscores, and hyphens");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters")
            .MaximumLength(128)
            .WithMessage("Password is too long")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]")
            .WithMessage("Password must contain at least one number")
            .Matches(@"[@$!%*?&#]")
            .WithMessage("Password must contain at least one special character (@$!%*?&#)");
    }
}
