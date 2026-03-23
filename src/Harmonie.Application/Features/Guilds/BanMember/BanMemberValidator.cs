using FluentValidation;

namespace Harmonie.Application.Features.Guilds.BanMember;

public sealed class BanMemberValidator : AbstractValidator<BanMemberRequest>
{
    public BanMemberValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid non-empty GUID");

        RuleFor(x => x.Reason)
            .MaximumLength(512)
            .WithMessage("Reason cannot exceed 512 characters")
            .When(x => x.Reason is not null);

        RuleFor(x => x.PurgeMessagesDays)
            .InclusiveBetween(0, 7)
            .WithMessage("Purge messages days must be between 0 and 7");
    }
}
