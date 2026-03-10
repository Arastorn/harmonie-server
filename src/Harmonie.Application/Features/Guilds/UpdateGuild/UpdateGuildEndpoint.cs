using FluentValidation;
using Harmonie.Application.Common;
using Harmonie.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Harmonie.Application.Features.Guilds.UpdateGuild;

public static class UpdateGuildEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/guilds/{guildId}", HandleAsync)
            .WithName("UpdateGuild")
            .WithTags("Guilds")
            .RequireAuthorization()
            .WithSummary("Update guild settings")
            .WithDescription("Updates guild name and icon settings. Only the guild owner or an admin can update guild settings.")
            .WithJsonRequestBodyDocumentation(
                "Partial guild update. Omit a field to keep its current value. Send `icon` as null to clear icon appearance. `name` cannot be null.",
                typeof(UpdateGuildOpenApiRequest),
                (
                    "updateIconAppearance",
                    "Update icon appearance only",
                    new
                    {
                        icon = new { color = "#7C3AED", name = "sword", bg = "#1F2937" }
                    }),
                (
                    "updateNameAndIconUrl",
                    "Update name and icon URL",
                    new
                    {
                        name = "My Guild",
                        iconUrl = "https://cdn.harmonie.chat/guild-icon.png"
                    }),
                (
                    "clearIcon",
                    "Clear icon URL and generated icon",
                    new
                    {
                        iconUrl = (string?)null,
                        icon = (object?)null
                    }))
            .Produces<UpdateGuildResponse>(StatusCodes.Status200OK)
            .ProducesErrors(
                ApplicationErrorCodes.Common.ValidationFailed,
                ApplicationErrorCodes.Auth.InvalidCredentials,
                ApplicationErrorCodes.Guild.NotFound,
                ApplicationErrorCodes.Guild.AccessDenied);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] UpdateGuildRouteRequest routeRequest,
        [FromBody] UpdateGuildRequest request,
        [FromServices] UpdateGuildHandler handler,
        [FromServices] IValidator<UpdateGuildRouteRequest> routeValidator,
        [FromServices] IValidator<UpdateGuildRequest> validator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var routeValidationError = await routeRequest.ValidateAsync(routeValidator, cancellationToken);
        if (routeValidationError is not null)
            return ApplicationResponse<UpdateGuildResponse>.Fail(routeValidationError).ToHttpResult();

        var validationError = await request.ValidateAsync(validator, cancellationToken);
        if (validationError is not null)
            return ApplicationResponse<UpdateGuildResponse>.Fail(validationError).ToHttpResult();

        if (routeRequest.GuildId is not string guildId
            || !GuildId.TryParse(guildId, out var parsedGuildId)
            || parsedGuildId is null)
        {
            return ApplicationResponse<UpdateGuildResponse>.Fail(
                ApplicationErrorCodes.Common.InvalidState,
                "Route validation succeeded but guild ID parsing failed.").ToHttpResult();
        }

        var callerId = httpContext.GetRequiredAuthenticatedUserId();

        var response = await handler.HandleAsync(parsedGuildId, callerId, request, cancellationToken);
        return response.ToHttpResult();
    }

    internal sealed record UpdateGuildOpenApiRequest(
        string? Name,
        string? IconUrl,
        UpdateGuildOpenApiIconRequest? Icon);

    internal sealed record UpdateGuildOpenApiIconRequest(
        string? Color,
        string? Name,
        string? Bg);
}
