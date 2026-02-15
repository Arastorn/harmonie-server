using Microsoft.AspNetCore.Routing;

namespace Harmonie.Application.Common;

/// <summary>
/// Marker interface for feature endpoints.
/// Used for automatic registration via reflection if needed.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Map the endpoint to the application
    /// </summary>
    static abstract void Map(IEndpointRouteBuilder app);
}
