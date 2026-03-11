using FluentValidation;

namespace Harmonie.Application.Features.Conversations.GetMessages;

public sealed class GetMessagesRouteValidator : AbstractValidator<GetMessagesRouteRequest>
{
    public GetMessagesRouteValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("Conversation ID is required")
            .Must(conversationId => Guid.TryParse(conversationId, out var parsed) && parsed != Guid.Empty)
            .WithMessage("Conversation ID must be a valid non-empty GUID");
    }
}
