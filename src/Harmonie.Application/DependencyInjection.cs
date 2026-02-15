using System.Reflection;
using FluentValidation;
using Harmonie.Application.Features.Auth.Login;
using Harmonie.Application.Features.Auth.Register;
using Microsoft.Extensions.DependencyInjection;

namespace Harmonie.Application;

/// <summary>
/// Extension methods for configuring Application layer services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register feature handlers
        // Auth features
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        // Add more handlers as features are created

        return services;
    }
}
