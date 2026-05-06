using Harmonie.Application.Common;
using Harmonie.Domain.ValueObjects.Conversations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Harmonie.Application.Features.Conversations.GetConversationVoiceParticipants;

public static class GetConversationVoiceParticipantsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/conversations/{conversationId}/voice/participants", HandleAsync)
            .WithName("GetConversationVoiceParticipants")
            .WithTags("Conversations")
            .RequireAuthorization()
            .WithSummary("Get active voice participants in a conversation")
            .WithDescription("Returns the list of participants currently in the conversation voice call.")
            .Produces<GetConversationVoiceParticipantsResponse>(StatusCodes.Status200OK)
            .ProducesErrors(
                ApplicationErrorCodes.Common.ValidationFailed,
                ApplicationErrorCodes.Auth.InvalidCredentials,
                ApplicationErrorCodes.Conversation.NotFound,
                ApplicationErrorCodes.Conversation.VoiceAccessDenied);
    }

    private static async Task<IResult> HandleAsync(
        ConversationId conversationId,
        [FromServices] IAuthenticatedHandler<ConversationId, GetConversationVoiceParticipantsResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var currentUserId = httpContext.GetRequiredAuthenticatedUserId();

        var response = await handler.HandleAsync(conversationId, currentUserId, cancellationToken);
        return response.ToHttpResult(httpContext);
    }
}
