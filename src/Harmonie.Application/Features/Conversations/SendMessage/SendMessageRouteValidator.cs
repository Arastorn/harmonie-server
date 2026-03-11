using FluentValidation;

namespace Harmonie.Application.Features.Conversations.SendMessage;

public sealed class SendMessageRouteValidator : AbstractValidator<SendMessageRouteRequest>
{
    public SendMessageRouteValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("Conversation ID is required")
            .Must(conversationId => Guid.TryParse(conversationId, out var parsed) && parsed != Guid.Empty)
            .WithMessage("Conversation ID must be a valid non-empty GUID");
    }
}
