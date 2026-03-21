using System.Text.RegularExpressions;
using Harmonie.Domain.Common;

namespace Harmonie.Domain.ValueObjects.Users;

/// <summary>
/// Username value object with validation rules.
/// Usernames must be alphanumeric with optional underscores and hyphens.
/// </summary>
public sealed record Username
{
    private static readonly Regex UsernameRegex = new(
        @"^[a-zA-Z0-9_-]+$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250));

    public const int MinLength = 3;
    public const int MaxLength = 32;

    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Create a Username from a string, validating the format
    /// </summary>
    public static Result<Username> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Username>("Username cannot be empty");

        value = value.Trim();

        if (value.Length < MinLength)
            return Result.Failure<Username>($"Username must be at least {MinLength} characters");

        if (value.Length > MaxLength)
            return Result.Failure<Username>($"Username cannot exceed {MaxLength} characters");

        if (!UsernameRegex.IsMatch(value))
            return Result.Failure<Username>("Username can only contain letters, numbers, underscores, and hyphens");

        if (value.StartsWith('_') || value.StartsWith('-'))
            return Result.Failure<Username>("Username cannot start with an underscore or hyphen");

        if (value.EndsWith('_') || value.EndsWith('-'))
            return Result.Failure<Username>("Username cannot end with an underscore or hyphen");

        return Result.Success(new Username(value));
    }

    public override string ToString() => Value;

    // Implicit conversion to string for convenience
    public static implicit operator string(Username username) => username.Value;
}
