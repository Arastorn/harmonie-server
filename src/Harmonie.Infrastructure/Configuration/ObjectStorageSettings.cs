namespace Harmonie.Infrastructure.Configuration;

public sealed class ObjectStorageSettings
{
    public string Endpoint { get; init; } = string.Empty;

    public string PublicBaseUrl { get; init; } = string.Empty;

    public string BucketName { get; init; } = string.Empty;

    public string Region { get; init; } = "garage";

    public string AccessKeyId { get; init; } = string.Empty;

    public string SecretAccessKey { get; init; } = string.Empty;

    public bool ForcePathStyle { get; init; } = true;

    public bool CreateBucketIfMissing { get; init; } = true;
}
