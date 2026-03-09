using Microsoft.AspNetCore.Http;

namespace Harmonie.Application.Features.Users.UploadMyAvatar;

public sealed class UploadMyAvatarRequest
{
    public IFormFile? File { get; init; }
}
