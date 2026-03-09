using System.ComponentModel.DataAnnotations;

namespace Harmonie.Infrastructure.Configuration;

public sealed class DatabaseSettings
{
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;
}
