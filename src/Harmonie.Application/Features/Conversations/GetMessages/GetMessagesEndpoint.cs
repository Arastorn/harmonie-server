using FluentValidation;
using Harmonie.Application.Common;
using Harmonie.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Harmonie.Application.Features.Conversations.GetMessages;

public static class GetMessagesEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/conversations/{conversationId}/messages", HandleAsync)
            .WithName("GetConversationMessages")
            .WithTags("Conversations")
            .RequireAuthorization()
            .WithSummary("Get conversation messages")
            .WithDescription("Returns messages in a conversation with cursor pagination.")
            .Produces<GetMessagesResponse>(StatusCodes.Status200OK)
            .ProducesErrors(
                ApplicationErrorCodes.Common.ValidationFailed,
                ApplicationErrorCodes.Auth.InvalidCredentials,
                ApplicationErrorCodes.Conversation.NotFound,
                ApplicationErrorCodes.Conversation.AccessDenied);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] GetMessagesRouteRequest routeRequest,
        [AsParameters] GetMessagesRequest request,
        [FromServices] GetMessagesHandler handler,
        [FromServices] IValidator<GetMessagesRouteRequest> routeValidator,
        [FromServices] IValidator<GetMessagesRequest> validator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var routeValidationError = await routeRequest.ValidateAsync(routeValidator, cancellationToken);
        if (routeValidationError is not null)
            return ApplicationResponse<GetMessagesResponse>.Fail(routeValidationError).ToHttpResult();

        var validationError = await request.ValidateAsync(validator, cancellationToken);
        if (validationError is not null)
            return ApplicationResponse<GetMessagesResponse>.Fail(validationError).ToHttpResult();

        if (routeRequest.ConversationId is not string conversationId
            || !ConversationId.TryParse(conversationId, out var parsedConversationId)
            || parsedConversationId is null)
        {
            return ApplicationResponse<GetMessagesResponse>.Fail(
                ApplicationErrorCodes.Common.InvalidState,
                "Route validation succeeded but conversation ID parsing failed.").ToHttpResult();
        }

        var currentUserId = httpContext.GetRequiredAuthenticatedUserId();

        var response = await handler.HandleAsync(parsedConversationId, request, currentUserId, cancellationToken);
        return response.ToHttpResult();
    }
}
