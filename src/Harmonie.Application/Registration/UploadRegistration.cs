using Harmonie.Application.Features.Uploads.DownloadFile;
using Harmonie.Application.Features.Uploads.UploadFile;
using Microsoft.Extensions.DependencyInjection;

namespace Harmonie.Application.Registration;

public static class UploadRegistration
{
    public static IServiceCollection AddUploadHandlers(this IServiceCollection services)
    {
        services.AddScoped<UploadFileHandler>();
        services.AddScoped<DownloadFileHandler>();

        return services;
    }
}
