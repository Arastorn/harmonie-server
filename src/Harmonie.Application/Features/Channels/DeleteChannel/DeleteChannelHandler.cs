using Harmonie.Application.Common;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Channels.DeleteChannel;

public sealed class DeleteChannelHandler
{
    private readonly IGuildChannelRepository _guildChannelRepository;
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteChannelHandler> _logger;

    public DeleteChannelHandler(
        IGuildChannelRepository guildChannelRepository,
        IGuildMemberRepository guildMemberRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteChannelHandler> logger)
    {
        _guildChannelRepository = guildChannelRepository;
        _guildMemberRepository = guildMemberRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        GuildChannelId channelId,
        UserId callerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DeleteChannel started. ChannelId={ChannelId}, CallerId={CallerId}",
            channelId,
            callerId);

        var channel = await _guildChannelRepository.GetByIdAsync(channelId, cancellationToken);
        if (channel is null)
        {
            _logger.LogWarning(
                "DeleteChannel failed because channel was not found. ChannelId={ChannelId}",
                channelId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Channel.NotFound,
                "Channel was not found");
        }

        var role = await _guildMemberRepository.GetRoleAsync(channel.GuildId, callerId, cancellationToken);
        if (role is null)
        {
            _logger.LogWarning(
                "DeleteChannel failed because caller is not a member. ChannelId={ChannelId}, GuildId={GuildId}, CallerId={CallerId}",
                channelId,
                channel.GuildId,
                callerId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Channel.AccessDenied,
                "You do not have access to this channel");
        }

        if (role != GuildRole.Admin)
        {
            _logger.LogWarning(
                "DeleteChannel failed because caller is not an admin. ChannelId={ChannelId}, GuildId={GuildId}, CallerId={CallerId}, Role={Role}",
                channelId,
                channel.GuildId,
                callerId,
                role);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Guild.AccessDenied,
                "Only guild admins can delete channels");
        }

        if (channel.IsDefault)
        {
            _logger.LogWarning(
                "DeleteChannel failed because channel is the default channel. ChannelId={ChannelId}, GuildId={GuildId}",
                channelId,
                channel.GuildId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Channel.CannotDeleteDefault,
                "The default channel cannot be deleted");
        }

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken);
        await _guildChannelRepository.DeleteAsync(channelId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "DeleteChannel succeeded. ChannelId={ChannelId}, CallerId={CallerId}",
            channelId,
            callerId);

        return ApplicationResponse<bool>.Ok(true);
    }
}
