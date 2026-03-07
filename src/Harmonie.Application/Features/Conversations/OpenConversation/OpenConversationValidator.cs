using FluentValidation;

namespace Harmonie.Application.Features.Conversations.OpenConversation;

public sealed class OpenConversationValidator : AbstractValidator<OpenConversationRequest>
{
    public OpenConversationValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty()
            .WithMessage("Target user ID is required")
            .Must(targetUserId => Guid.TryParse(targetUserId, out var parsed) && parsed != Guid.Empty)
            .WithMessage("Target user ID must be a valid non-empty GUID");
    }
}
