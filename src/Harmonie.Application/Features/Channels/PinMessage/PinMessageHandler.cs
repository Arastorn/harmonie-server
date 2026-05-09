using Harmonie.Application.Common;
using Harmonie.Application.Common.Messages;
using Harmonie.Application.Features.Channels.Pins;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Domain.ValueObjects.Channels;
using Harmonie.Domain.ValueObjects.Messages;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Channels.PinMessage;

public sealed record ChannelPinMessageInput(GuildChannelId ChannelId, MessageId MessageId);

public sealed class PinMessageHandler : IAuthenticatedHandler<ChannelPinMessageInput, bool>
{
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IPinNotifier _pinNotifier;
    private readonly ILogger<ChannelPinScope> _scopeLogger;
    private readonly PinOrchestrator _orchestrator;

    public PinMessageHandler(
        IGuildChannelRepository guildChannelRepository,
        IPinNotifier pinNotifier,
        ILogger<ChannelPinScope> scopeLogger,
        PinOrchestrator orchestrator)
    {
        _guildChannelRepository = guildChannelRepository;
        _pinNotifier = pinNotifier;
        _scopeLogger = scopeLogger;
        _orchestrator = orchestrator;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        ChannelPinMessageInput request,
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = new ChannelPinScope(
            request.ChannelId, _guildChannelRepository, _pinNotifier, _scopeLogger);

        return await _orchestrator.PinAsync(
            scope,
            new MessageScope.Channel(request.ChannelId),
            request.MessageId,
            currentUserId,
            cancellationToken);
    }
}
