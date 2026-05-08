using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Features.Channels.Reactions;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Channels.RemoveReaction;

public sealed record ChannelRemoveReactionInput(GuildChannelId ChannelId, MessageId MessageId, string Emoji);

public sealed class RemoveReactionHandler : IAuthenticatedHandler<ChannelRemoveReactionInput, bool>
{
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IReactionNotifier _reactionNotifier;
    private readonly ILogger<ChannelReactionScope> _scopeLogger;
    private readonly ReactionOrchestrator _orchestrator;

    public RemoveReactionHandler(
        IGuildChannelRepository guildChannelRepository,
        IReactionNotifier reactionNotifier,
        ILogger<ChannelReactionScope> scopeLogger,
        ReactionOrchestrator orchestrator)
    {
        _guildChannelRepository = guildChannelRepository;
        _reactionNotifier = reactionNotifier;
        _scopeLogger = scopeLogger;
        _orchestrator = orchestrator;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        ChannelRemoveReactionInput request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = new ChannelReactionScope(
            request.ChannelId, _guildChannelRepository, _reactionNotifier, _scopeLogger);

        return await _orchestrator.RemoveAsync(
            scope,
            new MessageScope.Channel(request.ChannelId),
            request.MessageId,
            request.Emoji,
            currentUserId,
            cancellationToken);
    }
}
