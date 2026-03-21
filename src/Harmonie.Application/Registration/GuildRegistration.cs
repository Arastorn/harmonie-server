using Harmonie.Application.Features.Guilds.AcceptInvite;
using Harmonie.Application.Features.Guilds.BanMember;
using Harmonie.Application.Features.Guilds.CreateChannel;
using Harmonie.Application.Features.Guilds.CreateGuild;
using Harmonie.Application.Features.Guilds.CreateGuildInvite;
using Harmonie.Application.Features.Guilds.DeleteGuild;
using Harmonie.Application.Features.Guilds.DeleteGuildIcon;
using Harmonie.Application.Features.Guilds.GetGuildChannels;
using Harmonie.Application.Features.Guilds.GetGuildMembers;
using Harmonie.Application.Features.Guilds.GetGuildVoiceParticipants;
using Harmonie.Application.Features.Guilds.InviteMember;
using Harmonie.Application.Features.Guilds.LeaveGuild;
using Harmonie.Application.Features.Guilds.ListBans;
using Harmonie.Application.Features.Guilds.ListGuildInvites;
using Harmonie.Application.Features.Guilds.ListUserGuilds;
using Harmonie.Application.Features.Guilds.PreviewInvite;
using Harmonie.Application.Features.Guilds.RemoveMember;
using Harmonie.Application.Features.Guilds.ReorderChannels;
using Harmonie.Application.Features.Guilds.RevokeInvite;
using Harmonie.Application.Features.Guilds.SearchMessages;
using Harmonie.Application.Features.Guilds.TransferOwnership;
using Harmonie.Application.Features.Guilds.UnbanMember;
using Harmonie.Application.Features.Guilds.UpdateGuild;
using Harmonie.Application.Features.Guilds.UpdateMemberRole;
using Microsoft.Extensions.DependencyInjection;

namespace Harmonie.Application.Registration;

public static class GuildRegistration
{
    public static IServiceCollection AddGuildHandlers(this IServiceCollection services)
    {
        services.AddScoped<CreateGuildHandler>();
        services.AddScoped<DeleteGuildHandler>();
        services.AddScoped<DeleteGuildIconHandler>();
        services.AddScoped<ListUserGuildsHandler>();
        services.AddScoped<UpdateGuildHandler>();
        services.AddScoped<TransferOwnershipHandler>();
        services.AddScoped<SearchMessagesHandler>();

        // Channels within guilds
        services.AddScoped<CreateChannelHandler>();
        services.AddScoped<GetGuildChannelsHandler>();
        services.AddScoped<ReorderChannelsHandler>();

        // Members
        services.AddScoped<GetGuildMembersHandler>();
        services.AddScoped<InviteMemberHandler>();
        services.AddScoped<RemoveMemberHandler>();
        services.AddScoped<LeaveGuildHandler>();
        services.AddScoped<UpdateMemberRoleHandler>();

        // Bans
        services.AddScoped<BanMemberHandler>();
        services.AddScoped<ListBansHandler>();
        services.AddScoped<UnbanMemberHandler>();

        // Invites
        services.AddScoped<CreateGuildInviteHandler>();
        services.AddScoped<ListGuildInvitesHandler>();
        services.AddScoped<PreviewInviteHandler>();
        services.AddScoped<AcceptInviteHandler>();
        services.AddScoped<RevokeInviteHandler>();

        // Voice
        services.AddScoped<GetGuildVoiceParticipantsHandler>();

        return services;
    }
}
