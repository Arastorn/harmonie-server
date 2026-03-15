using Harmonie.Application.Common;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.Enums;
using Harmonie.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Guilds.UnbanMember;

public sealed class UnbanMemberHandler
{
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildBanRepository _guildBanRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnbanMemberHandler> _logger;

    public UnbanMemberHandler(
        IGuildRepository guildRepository,
        IGuildBanRepository guildBanRepository,
        IUnitOfWork unitOfWork,
        ILogger<UnbanMemberHandler> logger)
    {
        _guildRepository = guildRepository;
        _guildBanRepository = guildBanRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        GuildId guildId,
        UserId callerId,
        UserId targetId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "UnbanMember started. GuildId={GuildId}, CallerId={CallerId}, TargetId={TargetId}",
            guildId,
            callerId,
            targetId);

        var ctx = await _guildRepository.GetWithCallerRoleAsync(guildId, callerId, cancellationToken);
        if (ctx is null)
        {
            _logger.LogWarning(
                "UnbanMember failed because guild was not found. GuildId={GuildId}",
                guildId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Guild.NotFound,
                "Guild was not found");
        }

        if (ctx.CallerRole is null || ctx.CallerRole != GuildRole.Admin)
        {
            _logger.LogWarning(
                "UnbanMember failed because caller is not an admin. GuildId={GuildId}, CallerId={CallerId}",
                guildId,
                callerId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Guild.AccessDenied,
                "You must be an admin to unban members from this guild");
        }

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken);

        var deleted = await _guildBanRepository.DeleteAsync(guildId, targetId, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning(
                "UnbanMember failed because user is not banned. GuildId={GuildId}, TargetId={TargetId}",
                guildId,
                targetId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Guild.NotBanned,
                "User is not banned from this guild");
        }

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "UnbanMember succeeded. GuildId={GuildId}, CallerId={CallerId}, TargetId={TargetId}",
            guildId,
            callerId,
            targetId);

        return ApplicationResponse<bool>.Ok(true);
    }
}
