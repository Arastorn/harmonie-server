using FluentValidation;

namespace Harmonie.Application.Features.Users.SearchUsers;

public sealed class SearchUsersValidator : AbstractValidator<SearchUsersRequest>
{
    public SearchUsersValidator()
    {
        RuleFor(x => x.Q)
            .Must(query => !string.IsNullOrWhiteSpace(query) && query.Trim().Length >= 2)
            .WithMessage("Search query must contain at least 2 characters");

        RuleFor(x => x.GuildId)
            .NotEqual(Guid.Empty)
            .When(x => x.GuildId.HasValue)
            .WithMessage("Guild ID must be a valid non-empty GUID");

        RuleFor(x => x.Limit)
            .Must(limit => limit is null || (limit >= 1 && limit <= 100))
            .WithMessage("Limit must be between 1 and 100");
    }
}
