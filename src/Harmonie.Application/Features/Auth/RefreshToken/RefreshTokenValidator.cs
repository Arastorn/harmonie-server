using FluentValidation;

namespace Harmonie.Application.Features.Auth.RefreshToken;

/// <summary>
/// Validator for RefreshTokenRequest.
/// </summary>
public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required");
    }
}
