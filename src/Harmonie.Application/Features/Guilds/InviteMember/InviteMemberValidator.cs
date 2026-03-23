using FluentValidation;

namespace Harmonie.Application.Features.Guilds.InviteMember;

public sealed class InviteMemberValidator : AbstractValidator<InviteMemberRequest>
{
    public InviteMemberValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid non-empty GUID");
    }
}
