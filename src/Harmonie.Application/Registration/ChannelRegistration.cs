using Harmonie.Application.Features.Channels.AcknowledgeRead;
using Harmonie.Application.Features.Channels.DeleteChannel;
using Harmonie.Application.Features.Channels.DeleteMessageAttachment;
using Harmonie.Application.Features.Channels.JoinVoiceChannel;
using Harmonie.Application.Features.Channels.UpdateChannel;
using ChannelAddReactionHandler = Harmonie.Application.Features.Channels.AddReaction.AddReactionHandler;
using ChannelDeleteMessageHandler = Harmonie.Application.Features.Channels.DeleteMessage.DeleteMessageHandler;
using ChannelEditMessageHandler = Harmonie.Application.Features.Channels.EditMessage.EditMessageHandler;
using ChannelGetMessagesHandler = Harmonie.Application.Features.Channels.GetMessages.GetMessagesHandler;
using ChannelRemoveReactionHandler = Harmonie.Application.Features.Channels.RemoveReaction.RemoveReactionHandler;
using ChannelSendMessageHandler = Harmonie.Application.Features.Channels.SendMessage.SendMessageHandler;
using Microsoft.Extensions.DependencyInjection;

namespace Harmonie.Application.Registration;

public static class ChannelRegistration
{
    public static IServiceCollection AddChannelHandlers(this IServiceCollection services)
    {
        services.AddScoped<UpdateChannelHandler>();
        services.AddScoped<DeleteChannelHandler>();
        services.AddScoped<JoinVoiceChannelHandler>();

        // Messages
        services.AddScoped<ChannelSendMessageHandler>();
        services.AddScoped<ChannelGetMessagesHandler>();
        services.AddScoped<ChannelEditMessageHandler>();
        services.AddScoped<ChannelDeleteMessageHandler>();
        services.AddScoped<DeleteMessageAttachmentHandler>();
        services.AddScoped<AcknowledgeReadHandler>();

        // Reactions
        services.AddScoped<ChannelAddReactionHandler>();
        services.AddScoped<ChannelRemoveReactionHandler>();

        return services;
    }
}
