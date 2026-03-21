using Harmonie.Application.Features.Users.DeleteMyAvatar;
using Harmonie.Application.Features.Users.GetMyProfile;
using Harmonie.Application.Features.Users.SearchUsers;
using Harmonie.Application.Features.Users.UpdateMyProfile;
using Harmonie.Application.Features.Users.UpdateUserStatus;
using Harmonie.Application.Features.Users.UploadMyAvatar;
using Microsoft.Extensions.DependencyInjection;

namespace Harmonie.Application.Registration;

public static class UserRegistration
{
    public static IServiceCollection AddUserHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetMyProfileHandler>();
        services.AddScoped<UpdateMyProfileHandler>();
        services.AddScoped<DeleteMyAvatarHandler>();
        services.AddScoped<UploadMyAvatarHandler>();
        services.AddScoped<SearchUsersHandler>();
        services.AddScoped<UpdateUserStatusHandler>();

        return services;
    }
}
