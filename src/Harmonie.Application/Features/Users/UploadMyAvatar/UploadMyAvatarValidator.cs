using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Harmonie.Application.Features.Users.UploadMyAvatar;

public sealed class UploadMyAvatarValidator : AbstractValidator<UploadMyAvatarRequest>
{
    private const long MaxFileSizeBytes = 5L * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp"
    };

    public UploadMyAvatarValidator()
    {
        RuleFor(x => x.File)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("File is required.")
            .Must(file => file is not null && file.Length > 0)
            .WithMessage("File is required.")
            .Must(file => file is not null && file.Length <= MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes} bytes.")
            .Must(file => file is not null && HasFileName(file))
            .WithMessage("File name is required.")
            .Must(file => file is not null && HasAllowedContentType(file))
            .WithMessage("Only PNG, JPEG, and WebP images are supported.");
    }

    private static bool HasFileName(IFormFile file)
        => !string.IsNullOrWhiteSpace(Path.GetFileName(file.FileName));

    private static bool HasAllowedContentType(IFormFile file)
        => !string.IsNullOrWhiteSpace(file.ContentType)
           && AllowedContentTypes.Contains(file.ContentType.Trim());
}
