using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Features.Channels.Reads;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;

namespace Harmonie.Application.Features.Channels.AcknowledgeRead;

public sealed record AcknowledgeChannelReadInput(GuildChannelId ChannelId, MessageId? MessageId);

public sealed class AcknowledgeReadHandler : IAuthenticatedHandler<AcknowledgeChannelReadInput, bool>
{
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IChannelReadStateRepository _channelReadStateRepository;
    private readonly ReadOrchestrator _orchestrator;

    public AcknowledgeReadHandler(
        IGuildChannelRepository guildChannelRepository,
        IMessageRepository messageRepository,
        IChannelReadStateRepository channelReadStateRepository,
        ReadOrchestrator orchestrator)
    {
        _guildChannelRepository = guildChannelRepository;
        _messageRepository = messageRepository;
        _channelReadStateRepository = channelReadStateRepository;
        _orchestrator = orchestrator;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        AcknowledgeChannelReadInput request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = new ChannelReadScope(
            request.ChannelId, _guildChannelRepository, _messageRepository, _channelReadStateRepository);

        return await _orchestrator.AcknowledgeAsync(
            scope,
            new MessageScope.Channel(request.ChannelId),
            request.MessageId,
            currentUserId,
            cancellationToken);
    }
}
