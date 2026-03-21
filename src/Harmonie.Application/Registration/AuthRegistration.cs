using Harmonie.Application.Features.Auth.Login;
using Harmonie.Application.Features.Auth.Logout;
using Harmonie.Application.Features.Auth.LogoutAll;
using Harmonie.Application.Features.Auth.RefreshToken;
using Harmonie.Application.Features.Auth.Register;
using Microsoft.Extensions.DependencyInjection;

namespace Harmonie.Application.Registration;

public static class AuthRegistration
{
    public static IServiceCollection AddAuthHandlers(this IServiceCollection services)
    {
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<LogoutAllHandler>();
        services.AddScoped<RefreshTokenHandler>();

        return services;
    }
}
