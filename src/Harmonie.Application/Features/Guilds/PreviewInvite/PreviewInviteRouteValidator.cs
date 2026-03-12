using FluentValidation;

namespace Harmonie.Application.Features.Guilds.PreviewInvite;

public sealed class PreviewInviteRouteValidator : AbstractValidator<PreviewInviteRouteRequest>
{
    public PreviewInviteRouteValidator()
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
