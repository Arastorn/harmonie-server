using FluentValidation;

namespace Harmonie.Application.Features.Guilds.GetGuildVoiceParticipants;

public sealed class GetGuildVoiceParticipantsValidator : AbstractValidator<GetGuildVoiceParticipantsRequest>
{
    public GetGuildVoiceParticipantsValidator()
    {
        RuleFor(x => x.GuildId)
            .NotEmpty()
            .WithMessage("Guild ID is required")
            .Must(guildId => Guid.TryParse(guildId, out var parsed) && parsed != Guid.Empty)
            .WithMessage("Guild ID must be a valid non-empty GUID");
    }
}
