using System.ComponentModel.DataAnnotations;

namespace Harmonie.Infrastructure.Configuration;

public sealed class ObjectStorageSettings
{
    [Required(AllowEmptyStrings = false)]
    public string LocalBasePath { get; init; } = "uploads";

    [Required(AllowEmptyStrings = false)]
    public string LocalBaseUrl { get; init; } = "http://localhost:5000/files";
}
