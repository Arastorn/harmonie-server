using Harmonie.Application.Common;
using Harmonie.Domain.ValueObjects.Conversations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Harmonie.Application.Features.Conversations.JoinConversationVoice;

public static class JoinConversationVoiceEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/conversations/{conversationId}/voice/join", HandleAsync)
            .WithName("JoinConversationVoice")
            .WithTags("Conversations")
            .RequireAuthorization()
            .WithSummary("Join a conversation voice call")
            .WithDescription("Returns a LiveKit access token for an authenticated participant in a conversation voice call.")
            .Produces<JoinConversationVoiceResponse>(StatusCodes.Status200OK)
            .ProducesErrors(
                ApplicationErrorCodes.Common.ValidationFailed,
                ApplicationErrorCodes.Auth.InvalidCredentials,
                ApplicationErrorCodes.Conversation.NotFound,
                ApplicationErrorCodes.Conversation.VoiceAccessDenied,
                ApplicationErrorCodes.User.NotFound);
    }

    private static async Task<IResult> HandleAsync(
        ConversationId conversationId,
        [FromServices] IAuthenticatedHandler<ConversationId, JoinConversationVoiceResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var currentUserId = httpContext.GetRequiredAuthenticatedUserId();

        var response = await handler.HandleAsync(conversationId, currentUserId, cancellationToken);
        return response.ToHttpResult(httpContext);
    }
}
