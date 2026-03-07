using Harmonie.Application.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Harmonie.Application.Features.Conversations.ListConversations;

public static class ListConversationsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/conversations", HandleAsync)
            .WithName("ListConversations")
            .WithTags("Conversations")
            .RequireAuthorization()
            .WithSummary("List current user conversations")
            .WithDescription("Returns direct conversations for the authenticated user with the other participant's basic profile info.")
            .Produces<ListConversationsResponse>(StatusCodes.Status200OK)
            .ProducesErrors(
                ApplicationErrorCodes.Auth.InvalidCredentials);
    }

    private static async Task<IResult> HandleAsync(
        [FromServices] ListConversationsHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.TryGetAuthenticatedUserId(out var currentUserId) || currentUserId is null)
        {
            return ApplicationResponse<ListConversationsResponse>.Fail(
                    ApplicationErrorCodes.Auth.InvalidCredentials,
                    "Authenticated user identifier is missing.")
                .ToHttpResult();
        }

        var response = await handler.HandleAsync(currentUserId, cancellationToken);
        return response.ToHttpResult();
    }
}
