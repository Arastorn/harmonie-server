using Harmonie.Application.Common;
using Harmonie.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Harmonie.Application.Features.Guilds.GetGuildMembers;

public static class GetGuildMembersEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/guilds/{guildId}/members", HandleAsync)
            .WithName("GetGuildMembers")
            .WithTags("Guilds")
            .RequireAuthorization()
            .WithSummary("List guild members")
            .WithDescription("Returns guild members for an authenticated guild member.")
            .Produces<GetGuildMembersResponse>(StatusCodes.Status200OK)
            .Produces<ApplicationError>(StatusCodes.Status400BadRequest)
            .Produces<ApplicationError>(StatusCodes.Status401Unauthorized)
            .Produces<ApplicationError>(StatusCodes.Status403Forbidden)
            .Produces<ApplicationError>(StatusCodes.Status404NotFound)
            .Produces<ApplicationError>(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] string guildId,
        [FromServices] GetGuildMembersHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!GuildId.TryParse(guildId, out var parsedGuildId) || parsedGuildId is null)
        {
            var details = new Dictionary<string, string[]>
            {
                ["guildId"] = ["Guild ID must be a valid non-empty GUID"]
            };

            return ApplicationResponse<GetGuildMembersResponse>.Fail(
                ApplicationErrorCodes.Common.ValidationFailed,
                "Request validation failed",
                details).ToHttpResult();
        }

        if (!httpContext.TryGetAuthenticatedUserId(out var currentUserId) || currentUserId is null)
        {
            return ApplicationResponse<GetGuildMembersResponse>.Fail(
                    ApplicationErrorCodes.Auth.InvalidCredentials,
                    "Authenticated user identifier is missing.")
                .ToHttpResult();
        }

        var response = await handler.HandleAsync(parsedGuildId, currentUserId, cancellationToken);
        return response.ToHttpResult();
    }
}
