using System.ComponentModel.DataAnnotations;

namespace Harmonie.Infrastructure.Configuration;

public sealed class LiveKitSettings : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    public string PublicUrl { get; init; } = string.Empty;

    public string InternalUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ApiSecret { get; init; } = string.Empty;

    [Range(1, 30)]
    public int RequestTimeoutSeconds { get; init; } = 5;

    public string GetInternalUrl()
        => string.IsNullOrWhiteSpace(InternalUrl)
            ? PublicUrl
            : InternalUrl;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(PublicUrl, UriKind.Absolute, out _))
        {
            yield return new ValidationResult(
                "LiveKit:PublicUrl must be a valid absolute URL.",
                [nameof(PublicUrl)]);
        }

        if (!string.IsNullOrWhiteSpace(InternalUrl)
            && !Uri.TryCreate(InternalUrl, UriKind.Absolute, out _))
        {
            yield return new ValidationResult(
                "LiveKit:InternalUrl must be a valid absolute URL when provided.",
                [nameof(InternalUrl)]);
        }
    }
}
