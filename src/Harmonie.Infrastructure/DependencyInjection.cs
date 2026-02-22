using Harmonie.Application.Interfaces;
using Harmonie.Infrastructure.Authentication;
using Harmonie.Infrastructure.Configuration;
using Harmonie.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harmonie.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<DatabaseSettings>(configuration.GetSection("Database"));
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

        services.AddScoped<IUserRepository>(_ => new UserRepository(connectionString));
        services.AddScoped<IRefreshTokenRepository>(_ => new RefreshTokenRepository(connectionString));
        return services;
    }
}
