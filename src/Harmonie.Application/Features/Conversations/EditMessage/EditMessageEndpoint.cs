using FluentValidation;
using Harmonie.Application.Common;
using Harmonie.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Harmonie.Application.Features.Conversations.EditMessage;

public static class EditMessageEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/conversations/{conversationId}/messages/{messageId}", HandleAsync)
            .WithName("EditConversationMessage")
            .WithTags("Conversations")
            .RequireAuthorization()
            .WithSummary("Edit a conversation message")
            .WithDescription("Updates the content of a conversation message. Only the message author can edit their own messages.")
            .Produces<EditMessageResponse>(StatusCodes.Status200OK)
            .ProducesErrors(
                ApplicationErrorCodes.Common.ValidationFailed,
                ApplicationErrorCodes.Auth.InvalidCredentials,
                ApplicationErrorCodes.Common.DomainRuleViolation,
                ApplicationErrorCodes.Message.ContentEmpty,
                ApplicationErrorCodes.Message.ContentTooLong,
                ApplicationErrorCodes.Conversation.NotFound,
                ApplicationErrorCodes.Conversation.AccessDenied,
                ApplicationErrorCodes.Message.NotFound,
                ApplicationErrorCodes.Message.EditForbidden);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] EditMessageRouteRequest routeRequest,
        [FromBody] EditMessageRequest request,
        [FromServices] EditMessageHandler handler,
        [FromServices] IValidator<EditMessageRouteRequest> routeValidator,
        [FromServices] IValidator<EditMessageRequest> validator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var routeValidationError = await routeRequest.ValidateAsync(routeValidator, cancellationToken);
        if (routeValidationError is not null)
            return ApplicationResponse<EditMessageResponse>.Fail(routeValidationError).ToHttpResult();

        var validationError = await request.ValidateAsync(validator, cancellationToken);
        if (validationError is not null)
            return ApplicationResponse<EditMessageResponse>.Fail(validationError).ToHttpResult();

        if (routeRequest.ConversationId is not string conversationIdStr
            || !ConversationId.TryParse(conversationIdStr, out var parsedConversationId)
            || parsedConversationId is null)
        {
            return ApplicationResponse<EditMessageResponse>.Fail(
                ApplicationErrorCodes.Common.InvalidState,
                "Route validation succeeded but conversation ID parsing failed.").ToHttpResult();
        }

        if (routeRequest.MessageId is not string messageIdStr
            || !MessageId.TryParse(messageIdStr, out var parsedMessageId)
            || parsedMessageId is null)
        {
            return ApplicationResponse<EditMessageResponse>.Fail(
                ApplicationErrorCodes.Common.InvalidState,
                "Route validation succeeded but message ID parsing failed.").ToHttpResult();
        }

        var callerId = httpContext.GetRequiredAuthenticatedUserId();

        var response = await handler.HandleAsync(parsedConversationId, parsedMessageId, request, callerId, cancellationToken);
        return response.ToHttpResult();
    }
}
