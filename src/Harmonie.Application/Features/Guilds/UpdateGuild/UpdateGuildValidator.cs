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
    }

}
