using Harmonie.Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace Harmonie.API.Middleware;

public sealed class AuthenticatedUserContextMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticatedUserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresAuthorization(context))
        {
            await _next(context);
            return;
        }

        if (!context.TryGetAuthenticatedUserId(out var currentUserId) || currentUserId is null)
        {
            await EndpointExtensions.WriteErrorAsync(
                context.Response,
                new ApplicationError(
                    ApplicationErrorCodes.Auth.InvalidCredentials,
                    "Authenticated user identifier is missing."));
            return;
        }

        context.SetAuthenticatedUserId(currentUserId);
        await _next(context);
    }

    private static bool RequiresAuthorization(HttpContext context)
        => context.GetEndpoint()?.Metadata.GetMetadata<IAuthorizeData>() is not null;
}
