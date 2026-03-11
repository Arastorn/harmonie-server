using FluentValidation;

namespace Harmonie.Application.Features.Guilds.DeleteGuild;

public sealed class DeleteGuildRouteValidator : AbstractValidator<DeleteGuildRouteRequest>
{
    public DeleteGuildRouteValidator()
    {
        RuleFor(x => x.GuildId)
            .NotEmpty()
            .WithMessage("Guild ID is required")
            .Must(id => Guid.TryParse(id, out var parsed) && parsed != Guid.Empty)
            .WithMessage("Guild ID must be a valid non-empty GUID");
    }
}
