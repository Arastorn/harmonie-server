using FluentValidation;
namespace Harmonie.Application.Features.Guilds.UpdateGuild;

public sealed class UpdateGuildValidator : AbstractValidator<UpdateGuildRequest>
{
    public UpdateGuildValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Guild name is required")
            .When(x => x.NameIsSet);

        RuleFor(x => x.IconFileId)
            .NotEqual(Guid.Empty)
            .When(x => x.IconFileIdIsSet && x.IconFileId.HasValue)
            .WithMessage("Guild icon file ID must be a valid non-empty GUID");

        RuleFor(x => x.IconColor)
            .MaximumLength(50)
            .WithMessage("Guild icon color cannot exceed 50 characters")
            .When(x => x.IconColorIsSet && x.IconColor is not null);

        RuleFor(x => x.IconName)
            .MaximumLength(50)
            .WithMessage("Guild icon name cannot exceed 50 characters")
            .When(x => x.IconNameIsSet && x.IconName is not null);

        RuleFor(x => x.IconBg)
            .MaximumLength(50)
            .WithMessage("Guild icon background cannot exceed 50 characters")
            .When(x => x.IconBgIsSet && x.IconBg is not null);
    }

}
