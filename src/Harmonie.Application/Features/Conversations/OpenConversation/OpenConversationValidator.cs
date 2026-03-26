using FluentValidation;

namespace Harmonie.Application.Features.Conversations.OpenConversation;

public sealed class OpenConversationValidator : AbstractValidator<OpenConversationRequest>
{
    public OpenConversationValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEqual(Guid.Empty)
            .WithMessage("Target user ID must be a valid non-empty GUID");
    }
}
