using FluentValidation;
using Harmonie.Application.Common;
using Harmonie.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Harmonie.Application.Features.Guilds.GetGuildVoiceParticipants;

public static class GetGuildVoiceParticipantsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/guilds/{guildId}/voice/participants", HandleAsync)
            .WithName("GetGuildVoiceParticipants")
            .WithTags("Guilds")
            .RequireAuthorization()
            .WithSummary("List active voice participants for a guild")
            .WithDescription("Returns active LiveKit participants grouped by voice channel for an authenticated guild member.")
            .Produces<GetGuildVoiceParticipantsResponse>(StatusCodes.Status200OK)
            .ProducesErrors(
                ApplicationErrorCodes.Common.ValidationFailed,
                ApplicationErrorCodes.Auth.InvalidCredentials,
                ApplicationErrorCodes.Guild.NotFound,
                ApplicationErrorCodes.Guild.AccessDenied);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] GetGuildVoiceParticipantsRequest request,
        [FromServices] GetGuildVoiceParticipantsHandler handler,
        [FromServices] IValidator<GetGuildVoiceParticipantsRequest> validator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationError = await request.ValidateAsync(validator, cancellationToken);
        if (validationError is not null)
            return ApplicationResponse<GetGuildVoiceParticipantsResponse>.Fail(validationError).ToHttpResult();

        if (request.GuildId is not string guildId
            || !GuildId.TryParse(guildId, out var parsedGuildId)
            || parsedGuildId is null)
        {
            return ApplicationResponse<GetGuildVoiceParticipantsResponse>.Fail(
                ApplicationErrorCodes.Common.InvalidState,
                "Route validation succeeded but guild ID parsing failed.").ToHttpResult();
        }

        var currentUserId = httpContext.GetRequiredAuthenticatedUserId();

        var response = await handler.HandleAsync(parsedGuildId, currentUserId, cancellationToken);
        return response.ToHttpResult();
    }
}
