using System.Text.RegularExpressions;
using Harmonie.Domain.Common;

namespace Harmonie.Domain.ValueObjects;

/// <summary>
/// Email address value object with validation.
/// Ensures email addresses are always in a valid format.
/// </summary>
public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250));

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Create an Email from a string, validating the format
    /// </summary>
    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Email>("Email address cannot be empty");

        value = value.Trim().ToLowerInvariant();

        if (value.Length > 320) // RFC 5321
            return Result.Failure<Email>("Email address is too long (max 320 characters)");

        if (!EmailRegex.IsMatch(value))
            return Result.Failure<Email>("Email address format is invalid");

        return Result.Success(new Email(value));
    }

    public override string ToString() => Value;

    // Implicit conversion to string for convenience
    public static implicit operator string(Email email) => email.Value;
}
