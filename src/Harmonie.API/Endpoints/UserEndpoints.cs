using Harmonie.Application.Features.Users.DeleteMyAvatar;
using Harmonie.Application.Features.Users.GetMyProfile;
using Harmonie.Application.Features.Users.SearchUsers;
using Harmonie.Application.Features.Users.UpdateMyProfile;
using Harmonie.Application.Features.Users.UpdateUserStatus;
using Harmonie.Application.Features.Users.UploadMyAvatar;

namespace Harmonie.API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        GetMyProfileEndpoint.Map(app);
        UpdateMyProfileEndpoint.Map(app);
        DeleteMyAvatarEndpoint.Map(app);
        UploadMyAvatarEndpoint.Map(app);
        SearchUsersEndpoint.Map(app);
        UpdateUserStatusEndpoint.Map(app);
    }
}
