using Harmonie.Application.Features.Conversations.ListConversations;
using Harmonie.Application.Features.Conversations.OpenConversation;
using Harmonie.Application.Features.Conversations.SearchConversationMessages;
using ConversationAcknowledgeReadHandler = Harmonie.Application.Features.Conversations.AcknowledgeRead.AcknowledgeReadHandler;
using ConversationAddReactionHandler = Harmonie.Application.Features.Conversations.AddReaction.AddReactionHandler;
using ConversationDeleteMessageAttachmentHandler = Harmonie.Application.Features.Conversations.DeleteMessageAttachment.DeleteMessageAttachmentHandler;
using ConversationDeleteMessageHandler = Harmonie.Application.Features.Conversations.DeleteMessage.DeleteMessageHandler;
using ConversationEditMessageHandler = Harmonie.Application.Features.Conversations.EditMessage.EditMessageHandler;
using ConversationGetMessagesHandler = Harmonie.Application.Features.Conversations.GetMessages.GetMessagesHandler;
using ConversationRemoveReactionHandler = Harmonie.Application.Features.Conversations.RemoveReaction.RemoveReactionHandler;
using ConversationSendMessageHandler = Harmonie.Application.Features.Conversations.SendMessage.SendMessageHandler;
using Microsoft.Extensions.DependencyInjection;

namespace Harmonie.Application.Registration;

public static class ConversationRegistration
{
    public static IServiceCollection AddConversationHandlers(this IServiceCollection services)
    {
        services.AddScoped<OpenConversationHandler>();
        services.AddScoped<ListConversationsHandler>();
        services.AddScoped<SearchConversationMessagesHandler>();

        // Messages
        services.AddScoped<ConversationSendMessageHandler>();
        services.AddScoped<ConversationGetMessagesHandler>();
        services.AddScoped<ConversationEditMessageHandler>();
        services.AddScoped<ConversationDeleteMessageHandler>();
        services.AddScoped<ConversationDeleteMessageAttachmentHandler>();
        services.AddScoped<ConversationAcknowledgeReadHandler>();

        // Reactions
        services.AddScoped<ConversationAddReactionHandler>();
        services.AddScoped<ConversationRemoveReactionHandler>();

        return services;
    }
}
