using Harmonie.Application.Common;
using Harmonie.Application.Interfaces.Guilds;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Guilds.PreviewInvite;

public sealed class PreviewInviteHandler
{
    private readonly IGuildInviteRepository _guildInviteRepository;
    private readonly ILogger<PreviewInviteHandler> _logger;

    public PreviewInviteHandler(
        IGuildInviteRepository guildInviteRepository,
        ILogger<PreviewInviteHandler> logger)
    {
        _guildInviteRepository = guildInviteRepository;
        _logger = logger;
    }

    public async Task<ApplicationResponse<PreviewInviteResponse>> HandleAsync(
        string inviteCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PreviewInvite started. InviteCode={InviteCode}", inviteCode);

        var preview = await _guildInviteRepository.GetPreviewByCodeAsync(inviteCode, cancellationToken);
        if (preview is null)
        {
            _logger.LogWarning("PreviewInvite failed because invite was not found. InviteCode={InviteCode}", inviteCode);

            return ApplicationResponse<PreviewInviteResponse>.Fail(
                ApplicationErrorCodes.Invite.NotFound,
                "Invite was not found");
        }

        if (preview.ExpiresAtUtc.HasValue && preview.ExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            _logger.LogWarning(
                "PreviewInvite failed because invite has expired. InviteCode={InviteCode}, ExpiresAtUtc={ExpiresAtUtc}",
                inviteCode,
                preview.ExpiresAtUtc);

            return ApplicationResponse<PreviewInviteResponse>.Fail(
                ApplicationErrorCodes.Invite.Expired,
                "This invite has expired");
        }

        if (preview.MaxUses.HasValue && preview.UsesCount >= preview.MaxUses.Value)
        {
            _logger.LogWarning(
                "PreviewInvite failed because invite has reached max uses. InviteCode={InviteCode}, UsesCount={UsesCount}, MaxUses={MaxUses}",
                inviteCode,
                preview.UsesCount,
                preview.MaxUses);

            return ApplicationResponse<PreviewInviteResponse>.Fail(
                ApplicationErrorCodes.Invite.Exhausted,
                "This invite has reached its maximum number of uses");
        }

        GuildIconDto? guildIcon = null;
        if (preview.GuildIconColor is not null || preview.GuildIconName is not null || preview.GuildIconBg is not null)
        {
            guildIcon = new GuildIconDto(preview.GuildIconColor, preview.GuildIconName, preview.GuildIconBg);
        }

        _logger.LogInformation("PreviewInvite succeeded. InviteCode={InviteCode}", inviteCode);

        var payload = new PreviewInviteResponse(
            GuildName: preview.GuildName,
            GuildIconFileId: preview.GuildIconFileId?.ToString(),
            GuildIcon: guildIcon,
            MemberCount: preview.MemberCount,
            UsesCount: preview.UsesCount,
            MaxUses: preview.MaxUses,
            ExpiresAtUtc: preview.ExpiresAtUtc);

        return ApplicationResponse<PreviewInviteResponse>.Ok(payload);
    }
}
