using Harmonie.Application.Features.Auth.Login;
using Harmonie.Application.Features.Auth.Logout;
using Harmonie.Application.Features.Auth.LogoutAll;
using Harmonie.Application.Features.Auth.RefreshToken;
using Harmonie.Application.Features.Auth.Register;

namespace Harmonie.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RegisterEndpoint.Map(app);
        LoginEndpoint.Map(app);
        LogoutEndpoint.Map(app);
        LogoutAllEndpoint.Map(app);
        RefreshTokenEndpoint.Map(app);
    }
}
