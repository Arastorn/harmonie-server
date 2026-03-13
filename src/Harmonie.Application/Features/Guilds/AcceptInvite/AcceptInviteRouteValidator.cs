using FluentValidation;

namespace Harmonie.Application.Features.Guilds.AcceptInvite;

public sealed class AcceptInviteRouteValidator : AbstractValidator<AcceptInviteRouteRequest>
{
    public AcceptInviteRouteValidator()
    {
        RuleFor(x => x.InviteCode)
            .NotEmpty()
            .WithMessage("Invite code is required")
            .Length(8)
            .WithMessage("Invite code must be exactly 8 characters")
            .Matches("^[A-Za-z0-9]+$")
            .WithMessage("Invite code must be alphanumeric");
    }
}
