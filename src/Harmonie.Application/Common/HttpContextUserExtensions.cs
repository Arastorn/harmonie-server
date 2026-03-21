using System.Security.Claims;
using Harmonie.Domain.ValueObjects.Users;
using Microsoft.AspNetCore.Http;

namespace Harmonie.Application.Common;

public static class HttpContextUserExtensions
{
    private const string AuthenticatedUserIdItemKey = "__authenticated_user_id";

    public static bool TryGetAuthenticatedUserId(
        this HttpContext httpContext,
        out UserId? userId)
    {
        userId = null;

        if (httpContext is null)
            return false;

        var principal = httpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
            return false;

        var claimValue = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(claimValue))
            return false;

        if (!UserId.TryParse(claimValue, out var parsedUserId) || parsedUserId is null)
            return false;

        userId = parsedUserId;
        return true;
    }

    public static void SetAuthenticatedUserId(this HttpContext httpContext, UserId userId)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(userId);

        httpContext.Items[AuthenticatedUserIdItemKey] = userId;
    }

    public static UserId GetRequiredAuthenticatedUserId(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Items.TryGetValue(AuthenticatedUserIdItemKey, out var storedUserId)
            && storedUserId is UserId currentUserId)
        {
            return currentUserId;
        }

        if (httpContext.TryGetAuthenticatedUserId(out var parsedUserId) && parsedUserId is not null)
            return parsedUserId;

        throw new InvalidOperationException("Authenticated user identifier is missing from the current request.");
    }

    private static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
    {
        if (principal is null)
            return null;

        return principal.FindFirst(claimType)?.Value;
    }
}
